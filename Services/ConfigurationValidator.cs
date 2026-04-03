using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Config;
using BaseballCli.Models;

namespace BaseballCli.Services
{
    /// <summary>
    /// Comprehensive validation service for season configurations.
    /// Provides detailed error reporting and cross-validation of configuration elements.
    /// </summary>
    public class ConfigurationValidator
    {
        /// <summary>
        /// Validates a complete season configuration with detailed error reporting.
        /// </summary>
        public ValidationResult ValidateConfiguration(SeasonConfiguration config)
        {
            var result = new ValidationResult();

            if (config == null)
            {
                result.AddError("Configuration cannot be null");
                return result;
            }

            ValidateLeague(config.League, result);
            ValidateTeams(config.Teams, result);
            ValidateRules(config.Rules, result);
            ValidateProbabilityTables(config.ProbabilityTables, result);
            ValidateCrossReferences(config, result);

            return result;
        }

        private void ValidateLeague(LeagueConfig? league, ValidationResult result)
        {
            if (league == null)
            {
                result.AddError("League configuration is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(league.Name))
                result.AddError("League name cannot be empty");

            if (league.Name?.Length > 100)
                result.AddError("League name cannot exceed 100 characters");
        }

        private void ValidateTeams(List<TeamConfig>? teams, ValidationResult result)
        {
            if (teams == null || teams.Count == 0)
            {
                result.AddError("At least one team is required");
                return;
            }

            var teamNames = new HashSet<string>();
            var citiesPerTeam = new Dictionary<string, int>();

            for (int i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                string teamContext = $"Team {i + 1}";

                // Basic team validation
                if (string.IsNullOrWhiteSpace(team.Name))
                    result.AddError($"{teamContext}: Team name cannot be empty");
                else if (team.Name.Length > 50)
                    result.AddError($"{teamContext}: Team name cannot exceed 50 characters");
                else if (teamNames.Contains(team.Name))
                    result.AddError($"Duplicate team name: '{team.Name}'");
                else
                    teamNames.Add(team.Name);

                if (string.IsNullOrWhiteSpace(team.City))
                    result.AddError($"{teamContext}: City cannot be empty");
                else if (team.City.Length > 50)
                    result.AddError($"{teamContext}: City cannot exceed 50 characters");

                if (string.IsNullOrWhiteSpace(team.Manager))
                    result.AddError($"{teamContext}: Manager name cannot be empty");
                else if (team.Manager.Length > 50)
                    result.AddError($"{teamContext}: Manager name cannot exceed 50 characters");

                // Validate players
                ValidatePlayers(team.Players, teamContext, result);
            }

            // Check minimum teams for realistic league
            if (teams.Count < 2)
                result.AddWarning("A league typically has at least 2 teams");

            if (teams.Count > 50)
                result.AddWarning("More than 50 teams may impact simulation performance");
        }

        private void ValidatePlayers(List<PlayerConfig>? players, string teamContext, ValidationResult result)
        {
            if (players == null || players.Count == 0)
            {
                result.AddError($"{teamContext}: Team must have at least one player");
                return;
            }

            if (players.Count < 9)
                result.AddError($"{teamContext}: Team must have at least 9 players for a valid roster (got {players.Count})");

            if (players.Count > 50)
                result.AddWarning($"{teamContext}: Team has {players.Count} players (typical max is 40)");

            var playerNames = new HashSet<string>();
            var pitchersCount = 0;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                string playerContext = $"{teamContext} - Player {i + 1}";

                // Basic validation
                if (string.IsNullOrWhiteSpace(player.Name))
                    result.AddError($"{playerContext}: Player name cannot be empty");
                else if (player.Name.Length > 50)
                    result.AddError($"{playerContext}: Player name cannot exceed 50 characters");
                else if (playerNames.Contains(player.Name))
                    result.AddError($"{teamContext}: Duplicate player name '{player.Name}'");
                else
                    playerNames.Add(player.Name);

                // Gender validation
                if (string.IsNullOrWhiteSpace(player.Gender))
                    result.AddError($"{playerContext}: Gender must be specified");
                else if (player.Gender != "M" && player.Gender != "F")
                    result.AddError($"{playerContext}: Gender must be 'M' or 'F'");

                // Position validation
                if (string.IsNullOrWhiteSpace(player.Position))
                    result.AddError($"{playerContext}: Position cannot be empty");
                else if (!IsValidPosition(player.Position))
                    result.AddError($"{playerContext}: Invalid position '{player.Position}'");
                else if (player.Position == "Pitcher")
                    pitchersCount++;

                // Stats validation
                if (player.BattingAverage < 0 || player.BattingAverage > 0.400)
                    result.AddError($"{playerContext}: Batting average must be between 0 and 0.400");

                if (player.PowerRating < 0 || player.PowerRating > 1)
                    result.AddError($"{playerContext}: Power rating must be between 0 and 1");

                if (player.SpeedRating < 0 || player.SpeedRating > 1)
                    result.AddError($"{playerContext}: Speed rating must be between 0 and 1");

                if (player.FieldingAverage < 0.8 || player.FieldingAverage > 1)
                    result.AddError($"{playerContext}: Fielding average must be between 0.8 and 1.0");

                // Pitcher-specific validation
                if (player.Position == "Pitcher")
                {
                    if (player.PitchingSpeed == null || player.PitchingSpeed < 70 || player.PitchingSpeed > 105)
                        result.AddError($"{playerContext}: Pitcher must have valid pitching speed (70-105 mph)");

                    if (player.ControlRating == null || player.ControlRating < 0 || player.ControlRating > 1)
                        result.AddError($"{playerContext}: Pitcher must have control rating between 0 and 1");
                }
                else
                {
                    if (player.PitchingSpeed.HasValue)
                        result.AddWarning($"{playerContext}: Non-pitcher has pitching speed set (will be ignored)");

                    if (player.ControlRating.HasValue)
                        result.AddWarning($"{playerContext}: Non-pitcher has control rating set (will be ignored)");
                }

                // Salary validation
                if (player.Salary < 0)
                    result.AddError($"{playerContext}: Salary cannot be negative");

                if (player.Salary > 50000000)
                    result.AddWarning($"{playerContext}: Salary of ${player.Salary:N0} is unusually high");
            }

            // Check for minimum pitchers
            if (pitchersCount == 0)
                result.AddError($"{teamContext}: Team must have at least one pitcher");

            if (pitchersCount > 15)
                result.AddWarning($"{teamContext}: Team has {pitchersCount} pitchers (typical max is 12)");
        }

        private bool IsValidPosition(string position)
        {
            var validPositions = new[] { "Pitcher", "Catcher", "Infielder", "Outfielder" };
            return validPositions.Contains(position);
        }

        private void ValidateRules(RulesConfig? rules, ValidationResult result)
        {
            if (rules == null)
            {
                result.AddError("Rules configuration is required");
                return;
            }

            if (rules.SeasonLength <= 0 || rules.SeasonLength > 200)
                result.AddError("Season length must be between 1 and 200 games");

            if (rules.GamesPerSeries <= 0 || rules.GamesPerSeries > 7)
                result.AddError("Games per series must be between 1 and 7");

            if (rules.InningsPerGame <= 0 || rules.InningsPerGame > 15)
                result.AddError("Innings per game must be between 1 and 15");

            if (rules.InjuryRate < 0 || rules.InjuryRate > 0.5)
                result.AddError("Injury rate must be between 0 and 0.5 (0% to 50%)");

            if (rules.FatigueThreshold <= 0)
                result.AddError("Fatigue threshold must be greater than 0");

            if (string.IsNullOrWhiteSpace(rules.StartDate) || !DateTime.TryParse(rules.StartDate, out var startDate))
                result.AddError($"Start date '{rules.StartDate}' is not a valid date (use YYYY-MM-DD format)");
            else
            {
                if (startDate < DateTime.Now.AddDays(-365))
                    result.AddWarning("Start date is more than a year in the past");
                if (startDate > DateTime.Now.AddYears(10))
                    result.AddWarning("Start date is more than 10 years in the future");
            }

            if (rules.RandomSeed.HasValue && rules.RandomSeed < 0)
                result.AddWarning("Negative random seed will produce unpredictable results");
        }

        private void ValidateProbabilityTables(ProbabilityTablesConfig? tables, ValidationResult result)
        {
            if (tables == null)
            {
                result.AddError("Probability tables are required");
                return;
            }

            // Validate batting probabilities
            if (tables.Batting != null)
            {
                ValidateBattingProbabilities(tables.Batting, result);
            }

            // Validate pitching probabilities
            if (tables.Pitching != null)
            {
                ValidatePitchingProbabilities(tables.Pitching, result);
            }

            // Validate fielding probabilities
            if (tables.Fielding != null)
            {
                ValidateFieldingProbabilities(tables.Fielding, result);
            }
        }

        private void ValidateBattingProbabilities(BattingProbabilitiesConfig batting, ValidationResult result)
        {
            ValidateOutcomeProabilities(batting.RightVsRight, "Batting.RightVsRight", result);
            ValidateOutcomeProabilities(batting.RightVsLeft, "Batting.RightVsLeft", result);
            ValidateOutcomeProabilities(batting.LeftVsRight, "Batting.LeftVsRight", result);
            ValidateOutcomeProabilities(batting.LeftVsLeft, "Batting.LeftVsLeft", result);
        }

        private void ValidatePitchingProbabilities(PitchingProbabilitiesConfig pitching, ValidationResult result)
        {
            ValidateOutcomeProabilities(pitching.Fastball, "Pitching.Fastball", result);
            ValidateOutcomeProabilities(pitching.Offspeed, "Pitching.Offspeed", result);
        }

        private void ValidateFieldingProbabilities(FieldingProbabilitiesConfig fielding, ValidationResult result)
        {
            ValidateFieldingOutcomes(fielding.Infielder, "Fielding.Infielder", result);
            ValidateFieldingOutcomes(fielding.Outfielder, "Fielding.Outfielder", result);
        }

        private void ValidateOutcomeProabilities(OutcomeProabilities probs, string context, ValidationResult result)
        {
            if (probs == null)
            {
                result.AddError($"{context}: Outcome probabilities cannot be null");
                return;
            }

            double sum = probs.Strikeout + probs.Walk + probs.Single + probs.Double + 
                        probs.Triple + probs.HomeRun + probs.FieldersChoice + probs.Out;

            if (Math.Abs(sum - 1.0) > 0.01)
                result.AddError($"{context}: Probabilities must sum to 1.0 (got {sum:F3})");

            // Individual probability checks
            if (probs.Strikeout < 0 || probs.Strikeout > 0.5)
                result.AddWarning($"{context}.Strikeout: {probs.Strikeout:P} seems unusual");

            if (probs.HomeRun < 0 || probs.HomeRun > 0.15)
                result.AddWarning($"{context}.HomeRun: {probs.HomeRun:P} seems unusual");

            if (probs.Walk < 0 || probs.Walk > 0.25)
                result.AddWarning($"{context}.Walk: {probs.Walk:P} seems unusual");
        }

        private void ValidateFieldingOutcomes(FieldingOutcomes outcomes, string context, ValidationResult result)
        {
            if (outcomes == null)
            {
                result.AddError($"{context}: Fielding outcomes cannot be null");
                return;
            }

            double sum = outcomes.Out + outcomes.Error;
            if (Math.Abs(sum - 1.0) > 0.01)
                result.AddError($"{context}: Probabilities must sum to 1.0 (got {sum:F3})");

            if (outcomes.Error > 0.15)
                result.AddWarning($"{context}: Error rate of {outcomes.Error:P} seems high");
        }

        private void ValidateCrossReferences(SeasonConfiguration config, ValidationResult result)
        {
            if (config.Teams == null || config.Teams.Count == 0)
                return;

            // Check that all teams have valid player distributions
            int totalPlayers = config.Teams.Sum(t => t.Players?.Count ?? 0);
            if (totalPlayers == 0)
                result.AddError("At least one player must be defined across all teams");

            // Calculate total salary cap
            double totalSalary = config.Teams
                .SelectMany(t => t.Players ?? new List<PlayerConfig>())
                .Sum(p => p.Salary);

            if (totalSalary > 500000000)
                result.AddWarning($"Total payroll of ${totalSalary:N0} is very high");

            // Verify season length is reasonable given teams
            int numTeams = config.Teams.Count;
            int maxGamesPerTeam = (config.Rules?.SeasonLength ?? 162);
            int expectedTotalGames = (numTeams * (numTeams - 1) * (config.Rules?.GamesPerSeries ?? 3)) / 2;

            if (expectedTotalGames > maxGamesPerTeam * numTeams)
                result.AddWarning($"With {numTeams} teams, season length of {maxGamesPerTeam} may be too short");
        }
    }

    /// <summary>
    /// Result of configuration validation.
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool IsValid => Errors.Count == 0;
        public bool HasWarnings => Warnings.Count > 0;

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);

        public string GetReport(bool includeWarnings = true)
        {
            var lines = new List<string>();

            if (Errors.Count > 0)
            {
                lines.Add($"❌ VALIDATION ERRORS ({Errors.Count}):");
                foreach (var error in Errors)
                    lines.Add($"  • {error}");
            }

            if (includeWarnings && Warnings.Count > 0)
            {
                if (Errors.Count > 0)
                    lines.Add("");

                lines.Add($"⚠️  WARNINGS ({Warnings.Count}):");
                foreach (var warning in Warnings)
                    lines.Add($"  • {warning}");
            }

            if (Errors.Count == 0 && Warnings.Count == 0)
                lines.Add("✅ Configuration is valid");
            else if (Errors.Count == 0)
                lines.Add("✅ Configuration is valid (with warnings)");

            return string.Join("\n", lines);
        }

        public override string ToString() => GetReport();
    }
}
