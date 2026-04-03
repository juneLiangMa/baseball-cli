using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Config;
using BaseballCli.Models;

namespace BaseballCli.Services
{
    /// <summary>
    /// Algorithms for adjusting baseball event probabilities based on context, player stats, and game conditions.
    /// These algorithms are applied on top of base probability tables to create realistic gameplay.
    /// </summary>
    public class SimulationAlgorithms
    {
        private readonly RulesConfig _rules;
        private readonly Random _random;

        public SimulationAlgorithms(RulesConfig rules, int? seed = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Calculates fatigue modifier based on recent games played.
        /// Returns a multiplier (0.8 to 1.2) that affects performance.
        /// </summary>
        public double CalculateFatigueModifier(int? gamesPlayedRecently)
        {
            if (!gamesPlayedRecently.HasValue || gamesPlayedRecently.Value <= _rules.FatigueThreshold)
                return 1.0;

            int excessGames = gamesPlayedRecently.Value - _rules.FatigueThreshold;
            double fatigueModifier = 1.0 - (excessGames * 0.05);
            return Math.Max(0.8, Math.Min(1.2, fatigueModifier));
        }

        /// <summary>
        /// Calculates performance modifier based on time of season.
        /// Players tend to perform better mid-season and wear down at the end.
        /// </summary>
        public double CalculateSeasonalModifier(int dayOfSeason)
        {
            int seasonLength = _rules.SeasonLength;
            
            // Peak performance in middle third of season
            if (dayOfSeason <= seasonLength / 3)
                return 0.95; // Slow start
            if (dayOfSeason <= (2 * seasonLength / 3))
                return 1.05; // Peak season
            
            // Fatigue at end of season
            double endgameFatigue = (double)(dayOfSeason - (2 * seasonLength / 3)) / (seasonLength / 3);
            return Math.Max(0.90, 1.0 - (endgameFatigue * 0.10));
        }

        /// <summary>
        /// Calculates weather impact on gameplay.
        /// Wind and rain reduce hitting power and increase strikeouts.
        /// </summary>
        public WeatherImpact CalculateWeatherImpact(string weather)
        {
            return weather?.ToLower() switch
            {
                "clear" => new WeatherImpact { HittingModifier = 1.05, PitchingModifier = 0.98 },
                "cloudy" => new WeatherImpact { HittingModifier = 1.0, PitchingModifier = 1.0 },
                "rain" => new WeatherImpact { HittingModifier = 0.90, PitchingModifier = 1.08 },
                "windy" => new WeatherImpact { HittingModifier = 0.95, PitchingModifier = 1.05 },
                _ => new WeatherImpact { HittingModifier = 1.0, PitchingModifier = 1.0 }
            };
        }

        /// <summary>
        /// Calculates home field advantage modifier.
        /// Home teams get a slight boost to hitting and fielding.
        /// </summary>
        public double CalculateHomeFieldAdvantage(bool isHomeTeam)
        {
            return isHomeTeam ? 1.02 : 0.98;
        }

        /// <summary>
        /// Determines if a home run should be a "wall" instead (becomes a double/single).
        /// Parks and wind conditions affect this.
        /// </summary>
        public bool IsWalledOut(double ballDistance, string parkDimension = "Standard")
        {
            double wallHeight = parkDimension switch
            {
                "Tall" => 420,
                "Short" => 340,
                _ => 380 // Standard
            };

            return ballDistance < wallHeight && _random.NextDouble() < 0.15;
        }

        /// <summary>
        /// Simulates a batted ball distance based on exit velocity and angle.
        /// Used to determine home runs and extra-base hit outcomes.
        /// </summary>
        public double CalculateBallDistance(double powerRating, double batterSpeed = 90.0)
        {
            // Base exit velocity: 70-105 mph, average is 87 mph
            double exitVelocity = 75 + (powerRating * 30);
            
            // Add some randomness
            exitVelocity += (_random.NextDouble() - 0.5) * 10;

            // Rough formula: distance ≈ exit velocity * 1.3 + random factors
            double baseDistance = exitVelocity * 1.4;
            double randomFactor = (_random.NextDouble() - 0.5) * 20;

            return baseDistance + randomFactor;
        }

        /// <summary>
        /// Determines if an injury occurs during the game.
        /// </summary>
        public bool IsInjuryOccurrence()
        {
            return _random.NextDouble() < _rules.InjuryRate;
        }

        /// <summary>
        /// Calculates on-base percentage for a player.
        /// </summary>
        public double CalculateOnBasePercentage(double battingAverage, double baseWalkRate = 0.08)
        {
            // Estimate: (H + BB) / (AB + BB)
            // Assuming AB = 1, H = BA, BB = base walk rate
            return (battingAverage + baseWalkRate) / (1 + baseWalkRate);
        }

        /// <summary>
        /// Determines if a baserunner successfully steals a base.
        /// Depends on runner speed and pitcher control.
        /// </summary>
        public bool AttemptStolenBase(double runnerSpeed, double pitcherControlRating)
        {
            // Base success rate around 70%, modified by speed and pitcher control
            double successRate = 0.70 + (runnerSpeed - 0.5) * 0.20 - (pitcherControlRating * 0.15);
            return _random.NextDouble() < Math.Max(0.4, Math.Min(0.9, successRate));
        }

        /// <summary>
        /// Determines if a ground ball becomes a double play.
        /// Depends on runners on base, fielding ability, and randomness.
        /// </summary>
        public bool IsDoublePlaying(int outs, bool runnersInScoringPosition, double infielderFielding)
        {
            if (outs >= 2)
                return false; // Can't have double play with 2 outs

            double baseProbability = 0.15; // ~15% of ground balls are DPs
            double boost = runnersInScoringPosition ? 0.10 : 0;
            double fieldingBoost = (infielderFielding - 0.95) * 0.20;

            double probability = baseProbability + boost + fieldingBoost;
            return _random.NextDouble() < Math.Min(0.4, probability);
        }

        /// <summary>
        /// Calculates probability that a fly ball becomes an out vs a home run.
        /// </summary>
        public bool IsFlyBallOut(double hitDistance, double outfielderFielding)
        {
            // Most fly balls are outs unless they're home runs (300+ feet)
            if (hitDistance > 380) // Typical fence distance
                return false;

            // Outfielders with better fielding make outs on marginal flies
            double catchProbability = 0.85 + ((outfielderFielding - 0.95) * 0.15);
            return _random.NextDouble() < catchProbability;
        }

        /// <summary>
        /// Applies multiple modifiers to a base probability to get final probability.
        /// </summary>
        public double ApplyModifiers(double baseProbability, params double[] modifiers)
        {
            double result = baseProbability;
            foreach (var modifier in modifiers)
            {
                result *= modifier;
            }

            // Clamp to reasonable bounds
            return Math.Max(0.01, Math.Min(0.99, result));
        }

        /// <summary>
        /// Simulates a pitch to determine if it's a strike or ball.
        /// </summary>
        public bool IsPitchStrike(double controlRating, double battingAwareness = 0.5)
        {
            // Control rating directly affects strike probability
            double strikeProbability = 0.50 + (controlRating - 0.75) * 0.30;
            // Batter's awareness can reduce strike probability
            strikeProbability -= (battingAwareness * 0.10);

            return _random.NextDouble() < Math.Max(0.30, Math.Min(0.80, strikeProbability));
        }

        /// <summary>
        /// Determines how a fly ball is played based on distance and fielder stats.
        /// </summary>
        public FlyBallOutcome EvaluateFlyBall(double distance, double outfielderFielding)
        {
            if (distance > 400) return FlyBallOutcome.HomeRun;
            if (distance > 380) return FlyBallOutcome.DeepOut;
            if (distance > 250)
            {
                // Medium fly - depends on fielder
                bool catches = outfielderFielding > (_random.NextDouble() + 0.90);
                return catches ? FlyBallOutcome.Out : FlyBallOutcome.Single;
            }

            return FlyBallOutcome.Out; // Pop up
        }

        /// <summary>
        /// Determines how a ground ball is played based on fielder stats.
        /// </summary>
        public GroundBallOutcome EvaluateGroundBall(double infielderFielding, int outs, bool hasRunnerOnFirst)
        {
            double fieldingCheck = _random.NextDouble();
            
            // Excellent fielders make the play
            if (infielderFielding > fieldingCheck + 0.95)
                return GroundBallOutcome.Out;
            
            // Poor fielders make errors
            if (infielderFielding < fieldingCheck - 0.05)
                return GroundBallOutcome.Error;
            
            // Check for double play opportunity
            if (outs < 2 && hasRunnerOnFirst && IsDoublePlaying(outs, false, infielderFielding))
                return GroundBallOutcome.DoublePlaying;

            // Otherwise, likely a single
            return GroundBallOutcome.Single;
        }
    }

    public struct WeatherImpact
    {
        public double HittingModifier { get; set; }
        public double PitchingModifier { get; set; }
    }

    public enum FlyBallOutcome
    {
        Out,
        DeepOut,
        Single,
        HomeRun
    }

    public enum GroundBallOutcome
    {
        Out,
        Single,
        Error,
        DoublePlaying
    }

    /// <summary>
    /// Context for complex at-bat calculations.
    /// </summary>
    public class SimulationContext
    {
        public int Inning { get; set; }
        public int Outs { get; set; }
        public int DayOfSeason { get; set; }
        public bool IsHomeTeam { get; set; }
        public string Weather { get; set; } = "Clear";
        public int? BatterGamesPlayedRecently { get; set; }
        public int? PitcherGamesPlayedRecently { get; set; }

        public double GetFatigueAdjustment(SimulationAlgorithms algorithms, bool batter)
        {
            var games = batter ? BatterGamesPlayedRecently : PitcherGamesPlayedRecently;
            return algorithms.CalculateFatigueModifier(games);
        }
    }
}
