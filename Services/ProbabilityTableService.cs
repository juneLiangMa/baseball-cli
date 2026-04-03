using System;
using System.Collections.Generic;
using BaseballCli.Config;

namespace BaseballCli.Services
{
    /// <summary>
    /// Manages and applies probability tables for baseball event outcomes.
    /// Provides methods to look up and apply probability adjustments based on player stats.
    /// </summary>
    public class ProbabilityTableService
    {
        private readonly ProbabilityTablesConfig _tables;
        private readonly Random _random;

        public ProbabilityTableService(ProbabilityTablesConfig tables, int? seed = null)
        {
            _tables = tables ?? throw new ArgumentNullException(nameof(tables));
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ============= BATTING OUTCOMES =============

        /// <summary>
        /// Determines a batting outcome based on batter/pitcher handedness.
        /// Returns event type: "Single", "Double", "HomeRun", "Strikeout", "Walk", "Out", "Error", "FieldersChoice"
        /// </summary>
        public string ResolveBattingOutcome(bool batterIsRight, bool pitcherIsRight)
        {
            var probabilities = GetBattingProbabilities(batterIsRight, pitcherIsRight);
            return SelectOutcome(probabilities);
        }

        private OutcomeProabilities GetBattingProbabilities(bool batterIsRight, bool pitcherIsRight)
        {
            return (batterIsRight, pitcherIsRight) switch
            {
                (true, true) => _tables.Batting.RightVsRight,
                (true, false) => _tables.Batting.RightVsLeft,
                (false, true) => _tables.Batting.LeftVsRight,
                (false, false) => _tables.Batting.LeftVsLeft,
            };
        }

        private string SelectOutcome(OutcomeProabilities probs)
        {
            double roll = _random.NextDouble();
            double cumulative = 0;

            if (AddProbability(ref cumulative, probs.Strikeout, roll)) return "Strikeout";
            if (AddProbability(ref cumulative, probs.Walk, roll)) return "Walk";
            if (AddProbability(ref cumulative, probs.Single, roll)) return "Single";
            if (AddProbability(ref cumulative, probs.Double, roll)) return "Double";
            if (AddProbability(ref cumulative, probs.Triple, roll)) return "Triple";
            if (AddProbability(ref cumulative, probs.HomeRun, roll)) return "HomeRun";
            if (AddProbability(ref cumulative, probs.FieldersChoice, roll)) return "FieldersChoice";
            
            return "Out"; // Catch-all for rounding errors
        }

        // ============= PITCHING OUTCOMES =============

        /// <summary>
        /// Determines a pitching outcome based on pitch type.
        /// </summary>
        public string ResolvePitchingOutcome(bool isFastball)
        {
            var probabilities = isFastball ? _tables.Pitching.Fastball : _tables.Pitching.Offspeed;
            return SelectOutcome(probabilities);
        }

        // ============= FIELDING OUTCOMES =============

        /// <summary>
        /// Determines if a fielder successfully makes an out or commits an error.
        /// </summary>
        public bool FieldingSuccessful(bool isInfielder)
        {
            var outcomes = isInfielder ? _tables.Fielding.Infielder : _tables.Fielding.Outfielder;
            double roll = _random.NextDouble();
            return roll <= outcomes.Out;
        }

        /// <summary>
        /// Gets the error probability for a fielder.
        /// </summary>
        public double GetErrorProbability(bool isInfielder)
        {
            return isInfielder ? _tables.Fielding.Infielder.Error : _tables.Fielding.Outfielder.Error;
        }

        // ============= PROBABILITY ADJUSTMENTS =============

        /// <summary>
        /// Adjusts batting outcome probabilities based on player stats.
        /// Higher batting average increases hit chances, power rating increases extra-base hits, etc.
        /// </summary>
        public AdjustedProbabilities AdjustBattingProbabilities(
            OutcomeProabilities baseProbabilities,
            double battingAverage,
            double powerRating,
            double speedRating,
            double controlledAdjustmentFactor = 0.15)
        {
            var adjusted = new AdjustedProbabilities(baseProbabilities);

            // Batting average primarily affects hit probability
            double avgAdjustment = (battingAverage - 0.250) * controlledAdjustmentFactor;
            
            // Power rating increases home run and extra-base hit chances
            double powerAdjustment = powerRating * controlledAdjustmentFactor;
            
            // Speed rating increases single/stolen base chances
            double speedAdjustment = speedRating * 0.05;

            // Apply adjustments while maintaining probability distribution
            adjusted.HomeRun += powerAdjustment * 0.06;
            adjusted.Double += powerAdjustment * 0.04;
            adjusted.Single += avgAdjustment * 0.10 + speedAdjustment;
            adjusted.Strikeout -= avgAdjustment * 0.08; // Better hitters strike out less
            adjusted.Walk += avgAdjustment * 0.05;

            adjusted.Normalize();
            return adjusted;
        }

        /// <summary>
        /// Adjusts pitching probabilities based on pitcher stats.
        /// Higher control rating and speed affect strikeout/walk rates.
        /// </summary>
        public AdjustedProbabilities AdjustPitchingProbabilities(
            OutcomeProabilities baseProbabilities,
            double pitchingSpeed,
            double controlRating,
            double speedNormalized = 92.0)
        {
            var adjusted = new AdjustedProbabilities(baseProbabilities);

            // Speed differential (normalized around 92 mph)
            double speedFactor = (pitchingSpeed - speedNormalized) / speedNormalized;
            
            // Control rating affects walk and strikeout rates
            double controlAdjustment = controlRating * 0.15;

            adjusted.Strikeout += speedFactor * 0.05 + controlAdjustment * 0.08;
            adjusted.Walk -= controlAdjustment * 0.06; // Better control = fewer walks
            adjusted.HomeRun -= speedFactor * 0.03; // Faster pitches give up fewer HRs
            adjusted.Single -= speedFactor * 0.02;

            adjusted.Normalize();
            return adjusted;
        }

        /// <summary>
        /// Calculates walk probability based on plate discipline and control.
        /// </summary>
        public double CalculateWalkProbability(double baseProbability, double controlRating, double selfControl = 0.5)
        {
            // selfControl represents batter's ability to lay off bad pitches (0-1)
            double adjustedWalk = baseProbability + (controlRating - 0.75) * 0.05 - (selfControl * 0.03);
            return Math.Max(0.05, Math.Min(0.25, adjustedWalk)); // Clamp between 5% and 25%
        }

        /// <summary>
        /// Generates a random outcome weighted by probabilities.
        /// </summary>
        public string RandomOutcome(OutcomeProabilities probabilities)
        {
            return SelectOutcome(probabilities);
        }

        private bool AddProbability(ref double cumulative, double probability, double roll)
        {
            cumulative += probability;
            return roll <= cumulative;
        }
    }

    /// <summary>
    /// Represents adjusted probabilities that maintain normalization.
    /// </summary>
    public class AdjustedProbabilities
    {
        public double Strikeout { get; set; }
        public double Walk { get; set; }
        public double Single { get; set; }
        public double Double { get; set; }
        public double Triple { get; set; }
        public double HomeRun { get; set; }
        public double FieldersChoice { get; set; }
        public double Out { get; set; }

        public AdjustedProbabilities(OutcomeProabilities original)
        {
            Strikeout = original.Strikeout;
            Walk = original.Walk;
            Single = original.Single;
            Double = original.Double;
            Triple = original.Triple;
            HomeRun = original.HomeRun;
            FieldersChoice = original.FieldersChoice;
            Out = original.Out;
        }

        /// <summary>
        /// Normalizes probabilities so they sum to 1.0.
        /// </summary>
        public void Normalize()
        {
            double sum = Strikeout + Walk + Single + Double + Triple + HomeRun + FieldersChoice + Out;
            
            if (sum <= 0) // Safety check
            {
                Strikeout = 0.15;
                Walk = 0.10;
                Single = 0.20;
                Double = 0.05;
                Triple = 0.01;
                HomeRun = 0.04;
                FieldersChoice = 0.15;
                Out = 0.30;
                return;
            }

            // Clamp negative values to 0
            Strikeout = Math.Max(0, Strikeout);
            Walk = Math.Max(0, Walk);
            Single = Math.Max(0, Single);
            Double = Math.Max(0, Double);
            Triple = Math.Max(0, Triple);
            HomeRun = Math.Max(0, HomeRun);
            FieldersChoice = Math.Max(0, FieldersChoice);
            Out = Math.Max(0, Out);

            // Recalculate sum after clamping
            sum = Strikeout + Walk + Single + Double + Triple + HomeRun + FieldersChoice + Out;

            if (sum > 0)
            {
                Strikeout /= sum;
                Walk /= sum;
                Single /= sum;
                Double /= sum;
                Triple /= sum;
                HomeRun /= sum;
                FieldersChoice /= sum;
                Out /= sum;
            }
        }

        public OutcomeProabilities ToOutcomeProabilities()
        {
            return new OutcomeProabilities
            {
                Strikeout = Strikeout,
                Walk = Walk,
                Single = Single,
                Double = Double,
                Triple = Triple,
                HomeRun = HomeRun,
                FieldersChoice = FieldersChoice,
                Out = Out
            };
        }
    }

    /// <summary>
    /// Context for a single at-bat, containing all relevant probability modifiers.
    /// </summary>
    public class AtBatContext
    {
        public Models.Player Batter { get; set; } = null!;
        public Models.Player Pitcher { get; set; } = null!;
        public int Inning { get; set; }
        public int Outs { get; set; }
        public string RunnersOnBase { get; set; } = "---";
        
        // Weather/context
        public string Weather { get; set; } = "Clear"; // Clear, Cloudy, Rain, Wind
        public int? BatterFatigue { get; set; } // Number of games played recently
        public int? PitcherFatigue { get; set; }
    }
}
