using System;
using System.Collections.Generic;
using BaseballCli.Config;
using BaseballCli.Models;
using BaseballCli.Database;

namespace BaseballCli.Services
{
    /// <summary>
    /// Combines probability tables and algorithms to generate realistic baseball events.
    /// This is the core of the hybrid simulation system.
    /// </summary>
    public class EventGenerator
    {
        private readonly ProbabilityTableService _probabilities;
        private readonly SimulationAlgorithms _algorithms;
        private readonly BaseballRepository _repository;

        public EventGenerator(
            ProbabilityTablesConfig probabilityTables,
            RulesConfig rules,
            BaseballRepository repository,
            int? seed = null)
        {
            _probabilities = new ProbabilityTableService(probabilityTables, seed);
            _algorithms = new SimulationAlgorithms(rules, seed);
            _repository = repository;
        }

        /// <summary>
        /// Generates a complete at-bat result given batter, pitcher, and context.
        /// Returns detailed outcome information.
        /// </summary>
        public AtBatResult GenerateAtBatResult(
            Player batter,
            Player pitcher,
            SimulationContext context)
        {
            var result = new AtBatResult
            {
                Batter = batter,
                Pitcher = pitcher,
                Context = context
            };

            // Get base probabilities based on handedness (assume right-handed for MVP)
            var baseProbabilities = _probabilities.GetBattingProbabilities(true, true);

            // Adjust for batter stats and fatigue
            var batterFatigue = _algorithms.CalculateFatigueModifier(context.BatterGamesPlayedRecently);
            var adjBatting = _probabilities.AdjustBattingProbabilities(
                baseProbabilities,
                batter.BattingAverage,
                batter.PowerRating,
                batter.SpeedRating
            );

            // Adjust for pitcher stats and fatigue
            var pitcherFatigue = _algorithms.CalculateFatigueModifier(context.PitcherGamesPlayedRecently);
            var adjPitching = _probabilities.AdjustPitchingProbabilities(
                adjBatting.ToOutcomeProabilities(),
                pitcher.PitchingSpeed ?? 90,
                pitcher.ControlRating ?? 0.75
            );

            // Apply environmental factors
            var weather = _algorithms.CalculateWeatherImpact(context.Weather);
            var homeAdvantage = _algorithms.CalculateHomeFieldAdvantage(context.IsHomeTeam);

            // Generate the outcome
            string outcomeType = _probabilities.RandomOutcome(adjPitching.ToOutcomeProabilities());
            result.EventType = outcomeType;

            // Determine play result and potential runs
            DeterminePlayResult(result, outcomeType, context, weather, batter, pitcher);

            return result;
        }

        private void DeterminePlayResult(
            AtBatResult result,
            string outcomeType,
            SimulationContext context,
            WeatherImpact weather,
            Player batter,
            Player pitcher)
        {
            switch (outcomeType)
            {
                case "Strikeout":
                    result.Result = "Strikeout";
                    result.BaseAwarded = 0;
                    result.RunsScored = 0;
                    break;

                case "Walk":
                    result.Result = "Walk";
                    result.BaseAwarded = 1;
                    result.RunsScored = 0;
                    break;

                case "Single":
                    result.Result = "Single";
                    result.BaseAwarded = 1;
                    result.RunsScored = 0;
                    // Check for scoring runner from second
                    if (context.Outs < 2)
                        result.RunsScored = 1; // Runner on 2nd usually scores
                    break;

                case "Double":
                    result.Result = "Double";
                    result.BaseAwarded = 2;
                    result.RunsScored = 0;
                    // Both runners score from 1st and 2nd
                    if (context.Outs < 2)
                        result.RunsScored = 2;
                    break;

                case "Triple":
                    result.Result = "Triple";
                    result.BaseAwarded = 3;
                    result.RunsScored = 3; // All runners score
                    break;

                case "HomeRun":
                    // Check for wall effect
                    double ballDistance = _algorithms.CalculateBallDistance(batter.PowerRating);
                    if (_algorithms.IsWalledOut(ballDistance))
                    {
                        result.Result = "Double";
                        result.BaseAwarded = 2;
                        result.RunsScored = 3; // HR + runners
                    }
                    else
                    {
                        result.Result = "HomeRun";
                        result.BaseAwarded = 4;
                        result.RunsScored = 4; // Batter + all runners
                    }
                    break;

                case "FieldersChoice":
                    result.Result = "FieldersChoice";
                    result.BaseAwarded = 1;
                    result.RunsScored = 0;
                    break;

                case "Out":
                    // Distinguish between fly out, ground out, etc.
                    double rand = new Random().NextDouble();
                    if (rand < 0.5)
                    {
                        result.Result = "FlyOut";
                        result.BaseAwarded = 0;
                        // Sac fly can score runner
                        result.RunsScored = context.Outs < 2 ? 1 : 0;
                    }
                    else
                    {
                        result.Result = "GroundOut";
                        result.BaseAwarded = 0;
                        result.RunsScored = 0;
                    }
                    break;

                default:
                    result.Result = "Out";
                    result.BaseAwarded = 0;
                    result.RunsScored = 0;
                    break;
            }
        }

        /// <summary>
        /// Generates a fielding play to determine if an error occurs.
        /// </summary>
        public FieldingResult GenerateFieldingPlay(
            Player fielder,
            bool isInfielder,
            string playType)
        {
            var result = new FieldingResult
            {
                Fielder = fielder,
                PlayType = playType
            };

            bool successful = _probabilities.FieldingSuccessful(isInfielder);
            result.IsSuccessful = successful;

            if (!successful)
            {
                result.Error = "Error";
            }
            else
            {
                result.Error = null;
            }

            return result;
        }

        /// <summary>
        /// Generates injury occurrence for a player.
        /// </summary>
        public bool CheckForInjury()
        {
            return _algorithms.IsInjuryOccurrence();
        }

        /// <summary>
        /// Generates attempt to steal a base.
        /// </summary>
        public bool AttemptStolenBase(Player runner, Player pitcher)
        {
            return _algorithms.AttemptStolenBase(runner.SpeedRating, pitcher.ControlRating ?? 0.75);
        }

        /// <summary>
        /// Generates a hit-by-pitch outcome.
        /// </summary>
        public HitByPitchResult GenerateHitByPitch(Player batter, Player pitcher)
        {
            var result = new HitByPitchResult
            {
                Batter = batter,
                Pitcher = pitcher
            };

            // Control rating affects HBP probability
            double hbpProbability = 0.01 * (1.0 - (pitcher.ControlRating ?? 0.75));
            result.IsHitByPitch = new Random().NextDouble() < hbpProbability;

            if (result.IsHitByPitch)
            {
                result.BaseAwarded = 1; // Batter takes first
            }

            return result;
        }
    }

    /// <summary>
    /// Result of an at-bat event.
    /// </summary>
    public class AtBatResult
    {
        public Player Batter { get; set; } = null!;
        public Player Pitcher { get; set; } = null!;
        public SimulationContext Context { get; set; } = null!;

        public string EventType { get; set; } = ""; // Strikeout, Walk, Single, Double, HomeRun, Out, etc.
        public string Result { get; set; } = ""; // Specific outcome (Single, FlyOut, GroundOut, etc.)
        public int BaseAwarded { get; set; } // 0 = out, 1 = first, 2 = double, 3 = triple, 4 = home run
        public int RunsScored { get; set; } // Total runs scored on this play (including other runners)
        public bool IsSacFly { get; set; } // Distinguishes sac fly from other fly outs
        public bool IsDoublePlaying { get; set; }

        public override string ToString()
        {
            return $"{Batter.Name} ({EventType}) - {Result}. Runs: {RunsScored}";
        }
    }

    /// <summary>
    /// Result of a fielding play.
    /// </summary>
    public class FieldingResult
    {
        public Player Fielder { get; set; } = null!;
        public string PlayType { get; set; } = ""; // Catch, Tag, Throw, etc.
        public bool IsSuccessful { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Result of a hit-by-pitch event.
    /// </summary>
    public class HitByPitchResult
    {
        public Player Batter { get; set; } = null!;
        public Player Pitcher { get; set; } = null!;
        public bool IsHitByPitch { get; set; }
        public int BaseAwarded { get; set; } = 0;
    }

    /// <summary>
    /// Stolen base attempt result.
    /// </summary>
    public class StolenBaseResult
    {
        public Player Runner { get; set; } = null!;
        public bool IsSuccessful { get; set; }
        public string? Outcome { get; set; } // "Safe", "Out", "Error"
    }

    /// <summary>
    /// Comprehensive play event combining all information.
    /// </summary>
    public class PlayEvent
    {
        public int Inning { get; set; }
        public int PlayNumber { get; set; }
        public string Description { get; set; } = "";
        public AtBatResult? AtBatResult { get; set; }
        public List<StolenBaseResult> StolenBaseAttempts { get; set; } = new();
        public List<FieldingResult> FieldingPlays { get; set; } = new();
        public int? InjuredPlayer { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return Description;
        }
    }
}
