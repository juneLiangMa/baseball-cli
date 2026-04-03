using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BaseballCli.Config
{
    /// <summary>
    /// Configuration format for defining a complete baseball season.
    /// This includes league setup, teams, players, rules, and probability tables.
    /// </summary>
    public class SeasonConfiguration
    {
        [JsonPropertyName("league")]
        public LeagueConfig League { get; set; } = new();

        [JsonPropertyName("teams")]
        public List<TeamConfig> Teams { get; set; } = new();

        [JsonPropertyName("rules")]
        public RulesConfig Rules { get; set; } = new();

        [JsonPropertyName("probabilityTables")]
        public ProbabilityTablesConfig ProbabilityTables { get; set; } = new();

        /// <summary>
        /// Validates the entire configuration for consistency and correctness.
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (League == null)
                errors.Add("League configuration is required");
            else
                errors.AddRange(League.Validate());

            if (Teams == null || Teams.Count == 0)
                errors.Add("At least one team is required");
            else
            {
                var teamNames = new HashSet<string>();
                foreach (var team in Teams)
                {
                    var teamErrors = team.Validate();
                    errors.AddRange(teamErrors);

                    if (teamNames.Contains(team.Name))
                        errors.Add($"Duplicate team name: {team.Name}");
                    else
                        teamNames.Add(team.Name);
                }
            }

            if (Rules == null)
                errors.Add("Rules configuration is required");
            else
                errors.AddRange(Rules.Validate());

            if (ProbabilityTables == null)
                errors.Add("Probability tables are required");
            else
                errors.AddRange(ProbabilityTables.Validate());

            return errors;
        }
    }

    public class LeagueConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "Default League";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("League name cannot be empty");
            return errors;
        }
    }

    public class TeamConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("city")]
        public string City { get; set; } = null!;

        [JsonPropertyName("manager")]
        public string Manager { get; set; } = null!;

        [JsonPropertyName("players")]
        public List<PlayerConfig> Players { get; set; } = new();

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Team name cannot be empty");
            if (string.IsNullOrWhiteSpace(City))
                errors.Add("Team city cannot be empty");
            if (string.IsNullOrWhiteSpace(Manager))
                errors.Add("Team manager name cannot be empty");

            if (Players == null || Players.Count == 0)
                errors.Add($"Team {Name} must have at least one player");
            else if (Players.Count < 9)
                errors.Add($"Team {Name} must have at least 9 players (got {Players.Count})");
            else
            {
                var playerNames = new HashSet<string>();
                foreach (var player in Players)
                {
                    var playerErrors = player.Validate();
                    errors.AddRange(playerErrors);

                    if (playerNames.Contains(player.Name))
                        errors.Add($"Duplicate player name in team {Name}: {player.Name}");
                    else
                        playerNames.Add(player.Name);
                }
            }

            return errors;
        }
    }

    public class PlayerConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("gender")]
        public string Gender { get; set; } = "M"; // "M" or "F"

        [JsonPropertyName("position")]
        public string Position { get; set; } = null!; // "Pitcher", "Catcher", "Infielder", "Outfielder"

        [JsonPropertyName("battingAverage")]
        public double BattingAverage { get; set; } = 0.250;

        [JsonPropertyName("powerRating")]
        public double PowerRating { get; set; } = 0.400; // 0-1 scale

        [JsonPropertyName("speedRating")]
        public double SpeedRating { get; set; } = 0.400; // 0-1 scale

        [JsonPropertyName("fieldingAverage")]
        public double FieldingAverage { get; set; } = 0.950;

        [JsonPropertyName("pitchingSpeed")]
        public double? PitchingSpeed { get; set; } // MPH, required if Pitcher

        [JsonPropertyName("controlRating")]
        public double? ControlRating { get; set; } // 0-1 scale, required if Pitcher

        [JsonPropertyName("salary")]
        public double Salary { get; set; } = 1000000;

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Player name cannot be empty");
            if (string.IsNullOrWhiteSpace(Position))
                errors.Add("Player position cannot be empty");
            if (BattingAverage < 0 || BattingAverage > 0.400)
                errors.Add($"Player {Name}: Batting average must be between 0 and 0.400");
            if (PowerRating < 0 || PowerRating > 1)
                errors.Add($"Player {Name}: Power rating must be between 0 and 1");
            if (SpeedRating < 0 || SpeedRating > 1)
                errors.Add($"Player {Name}: Speed rating must be between 0 and 1");
            if (FieldingAverage < 0.8 || FieldingAverage > 1)
                errors.Add($"Player {Name}: Fielding average must be between 0.8 and 1");
            if (Position == "Pitcher")
            {
                if (PitchingSpeed == null || PitchingSpeed <= 70 || PitchingSpeed > 105)
                    errors.Add($"Player {Name}: Pitcher must have pitching speed between 70-105 mph");
                if (ControlRating == null || ControlRating < 0 || ControlRating > 1)
                    errors.Add($"Player {Name}: Pitcher must have control rating between 0 and 1");
            }
            if (Salary < 0)
                errors.Add($"Player {Name}: Salary cannot be negative");

            return errors;
        }
    }

    public class RulesConfig
    {
        [JsonPropertyName("seasonLength")]
        public int SeasonLength { get; set; } = 162; // Standard MLB season

        [JsonPropertyName("gamesPerSeries")]
        public int GamesPerSeries { get; set; } = 3; // Best of 3

        [JsonPropertyName("inningsPerGame")]
        public int InningsPerGame { get; set; } = 9;

        [JsonPropertyName("randomSeed")]
        public int? RandomSeed { get; set; } // Optional, for reproducible seasons

        [JsonPropertyName("injuryRate")]
        public double InjuryRate { get; set; } = 0.02; // 2% chance per game

        [JsonPropertyName("fatigueThreshold")]
        public int FatigueThreshold { get; set; } = 5; // Games played in last 7 days before fatigue kicks in

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; } = "2024-04-01";

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (SeasonLength <= 0 || SeasonLength > 200)
                errors.Add("Season length must be between 1 and 200");
            if (GamesPerSeries <= 0 || GamesPerSeries > 7)
                errors.Add("Games per series must be between 1 and 7");
            if (InningsPerGame <= 0 || InningsPerGame > 15)
                errors.Add("Innings per game must be between 1 and 15");
            if (InjuryRate < 0 || InjuryRate > 0.5)
                errors.Add("Injury rate must be between 0 and 0.5");
            if (FatigueThreshold <= 0)
                errors.Add("Fatigue threshold must be positive");
            if (!DateTime.TryParse(StartDate, out _))
                errors.Add($"Start date '{StartDate}' is not a valid date");

            return errors;
        }
    }

    public class ProbabilityTablesConfig
    {
        [JsonPropertyName("batting")]
        public BattingProbabilitiesConfig Batting { get; set; } = new();

        [JsonPropertyName("pitching")]
        public PitchingProbabilitiesConfig Pitching { get; set; } = new();

        [JsonPropertyName("fielding")]
        public FieldingProbabilitiesConfig Fielding { get; set; } = new();

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (Batting != null)
                errors.AddRange(Batting.Validate());
            if (Pitching != null)
                errors.AddRange(Pitching.Validate());
            if (Fielding != null)
                errors.AddRange(Fielding.Validate());

            return errors;
        }
    }

    /// <summary>
    /// Batting outcome probabilities indexed by handedness and pitcher type.
    /// Example: "RightVsRight" contains probabilities for RHH vs RHP.
    /// </summary>
    public class BattingProbabilitiesConfig
    {
        [JsonPropertyName("rightVsRight")]
        public OutcomeProabilities RightVsRight { get; set; } = new();

        [JsonPropertyName("rightVsLeft")]
        public OutcomeProabilities RightVsLeft { get; set; } = new();

        [JsonPropertyName("leftVsRight")]
        public OutcomeProabilities LeftVsRight { get; set; } = new();

        [JsonPropertyName("leftVsLeft")]
        public OutcomeProabilities LeftVsLeft { get; set; } = new();

        public List<string> Validate()
        {
            var errors = new List<string>();
            errors.AddRange(RightVsRight?.Validate("RightVsRight") ?? new());
            errors.AddRange(RightVsLeft?.Validate("RightVsLeft") ?? new());
            errors.AddRange(LeftVsRight?.Validate("LeftVsRight") ?? new());
            errors.AddRange(LeftVsLeft?.Validate("LeftVsLeft") ?? new());
            return errors;
        }
    }

    /// <summary>
    /// Pitching outcome probabilities by pitch type.
    /// </summary>
    public class PitchingProbabilitiesConfig
    {
        [JsonPropertyName("fastball")]
        public OutcomeProabilities Fastball { get; set; } = new();

        [JsonPropertyName("offspeed")]
        public OutcomeProabilities Offspeed { get; set; } = new();

        public List<string> Validate()
        {
            var errors = new List<string>();
            errors.AddRange(Fastball?.Validate("Fastball") ?? new());
            errors.AddRange(Offspeed?.Validate("Offspeed") ?? new());
            return errors;
        }
    }

    /// <summary>
    /// Fielding outcome probabilities by fielder type.
    /// </summary>
    public class FieldingProbabilitiesConfig
    {
        [JsonPropertyName("infielder")]
        public FieldingOutcomes Infielder { get; set; } = new();

        [JsonPropertyName("outfielder")]
        public FieldingOutcomes Outfielder { get; set; } = new();

        public List<string> Validate()
        {
            var errors = new List<string>();
            errors.AddRange(Infielder?.Validate("Infielder") ?? new());
            errors.AddRange(Outfielder?.Validate("Outfielder") ?? new());
            return errors;
        }
    }

    /// <summary>
    /// Batting outcome probabilities (must sum to 1.0).
    /// </summary>
    public class OutcomeProabilities
    {
        [JsonPropertyName("strikeout")]
        public double Strikeout { get; set; } = 0.15;

        [JsonPropertyName("walk")]
        public double Walk { get; set; } = 0.10;

        [JsonPropertyName("single")]
        public double Single { get; set; } = 0.20;

        [JsonPropertyName("double")]
        public double Double { get; set; } = 0.05;

        [JsonPropertyName("triple")]
        public double Triple { get; set; } = 0.01;

        [JsonPropertyName("homeRun")]
        public double HomeRun { get; set; } = 0.04;

        [JsonPropertyName("fieldersChoice")]
        public double FieldersChoice { get; set; } = 0.15;

        [JsonPropertyName("out")]
        public double Out { get; set; } = 0.30;

        public List<string> Validate(string context)
        {
            var errors = new List<string>();
            var sum = Strikeout + Walk + Single + Double + Triple + HomeRun + FieldersChoice + Out;
            
            // Allow small floating-point errors
            if (Math.Abs(sum - 1.0) > 0.001)
                errors.Add($"{context} outcome probabilities must sum to 1.0 (got {sum:F3})");

            if (Strikeout < 0 || Walk < 0 || Single < 0 || Double < 0 || Triple < 0 || HomeRun < 0 || FieldersChoice < 0 || Out < 0)
                errors.Add($"{context} outcome probabilities cannot be negative");

            return errors;
        }
    }

    /// <summary>
    /// Fielding outcome probabilities.
    /// </summary>
    public class FieldingOutcomes
    {
        [JsonPropertyName("out")]
        public double Out { get; set; } = 0.95;

        [JsonPropertyName("error")]
        public double Error { get; set; } = 0.05;

        public List<string> Validate(string context)
        {
            var errors = new List<string>();
            var sum = Out + Error;

            if (Math.Abs(sum - 1.0) > 0.001)
                errors.Add($"{context} fielding probabilities must sum to 1.0 (got {sum:F3})");

            if (Out < 0 || Error < 0)
                errors.Add($"{context} fielding probabilities cannot be negative");

            return errors;
        }
    }
}
