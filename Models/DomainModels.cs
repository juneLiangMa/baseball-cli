using System;
using System.Collections.Generic;

namespace BaseballCli.Models
{
    public class League
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        public virtual ICollection<Game> Games { get; set; } = new List<Game>();
    }

    public class Team
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public uint LeagueId { get; set; }
        public string Name { get; set; } = null!;
        public string City { get; set; } = null!;
        public string ManagerName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public virtual League League { get; set; } = null!;
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
        public virtual ICollection<Game> HomeGames { get; set; } = new List<Game>();
        public virtual ICollection<Game> AwayGames { get; set; } = new List<Game>();
        public virtual ICollection<TeamStats> SeasonStats { get; set; } = new List<TeamStats>();
    }

    public class Player
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public uint TeamId { get; set; }
        public string Name { get; set; } = null!;
        public string Gender { get; set; } = null!; // "M" or "F"
        public string Position { get; set; } = null!; // "Pitcher", "Catcher", "Infielder", "Outfielder"
        
        // Batting stats
        public double BattingAverage { get; set; }
        public double PowerRating { get; set; } // 0-1 scale
        public double SpeedRating { get; set; } // 0-1 scale
        
        // Fielding stats
        public double FieldingAverage { get; set; }
        
        // Pitching stats
        public double? PitchingSpeed { get; set; } // MPH, nullable if not a pitcher
        public double? ControlRating { get; set; } // 0-1 scale
        
        public double Salary { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Team Team { get; set; } = null!;
        public virtual ICollection<SeasonStats> SeasonStats { get; set; } = new List<SeasonStats>();
        
        // Computed property for display
        public bool IsPitcher => Position == "Pitcher";
        
        // Validation method
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Player name cannot be empty");
            if (string.IsNullOrWhiteSpace(Position))
                errors.Add("Player position cannot be empty");
            if (BattingAverage < 0 || BattingAverage > 1)
                errors.Add("Batting average must be between 0 and 1");
            if (PowerRating < 0 || PowerRating > 1)
                errors.Add("Power rating must be between 0 and 1");
            if (SpeedRating < 0 || SpeedRating > 1)
                errors.Add("Speed rating must be between 0 and 1");
            if (FieldingAverage < 0.8 || FieldingAverage > 1)
                errors.Add("Fielding average must be between 0.8 and 1");
            if (IsPitcher && (PitchingSpeed == null || PitchingSpeed <= 0 || PitchingSpeed > 105))
                errors.Add("Pitcher must have valid pitching speed (1-105 mph)");
            if (IsPitcher && (ControlRating == null || ControlRating < 0 || ControlRating > 1))
                errors.Add("Pitcher must have valid control rating (0-1)");
            if (Salary < 0)
                errors.Add("Salary cannot be negative");
            
            return errors.Count == 0;
        }
    }

    public class Game
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public uint LeagueId { get; set; }
        public uint HomeTeamId { get; set; }
        public uint AwayTeamId { get; set; }
        public DateTime GameDate { get; set; }
        public int Season { get; set; }
        
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string Status { get; set; } = "NotStarted"; // NotStarted, InProgress, Completed
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual League League { get; set; } = null!;
        public virtual Team HomeTeam { get; set; } = null!;
        public virtual Team AwayTeam { get; set; } = null!;
        public virtual ICollection<Play> Plays { get; set; } = new List<Play>();

        public Team GetWinner()
        {
            if (HomeScore > AwayScore) return HomeTeam;
            if (AwayScore > HomeScore) return AwayTeam;
            return null!; // Tie or not completed
        }

        public bool IsComplete => Status == "Completed";
    }

    public class Play
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public uint GameId { get; set; }
        public int Inning { get; set; }
        public int PlayNumber { get; set; }
        
        public uint BatterId { get; set; }
        public uint PitcherId { get; set; }
        public uint BatterTeamId { get; set; }
        
        public string EventType { get; set; } = null!; // "AtBat", "Walk", "Error", etc.
        public string Result { get; set; } = null!; // "Single", "Double", "HomeRun", "Strikeout", "Out", etc.
        
        public int Outs { get; set; }
        public string? RunnersOnBase { get; set; } // Encoded runners: "---", "1--", "1-3", "123", etc.
        
        public DateTime CreatedAt { get; set; }

        public virtual Game Game { get; set; } = null!;
        public virtual Player Batter { get; set; } = null!;
        public virtual Player Pitcher { get; set; } = null!;
        public virtual Team BatterTeam { get; set; } = null!;
    }

    public class SeasonStats
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public uint PlayerId { get; set; }
        public uint TeamId { get; set; }
        public int Season { get; set; }

        // Batting stats
        public int GamesPlayed { get; set; }
        public int AtBats { get; set; }
        public int Hits { get; set; }
        public int Doubles { get; set; }
        public int Triples { get; set; }
        public int HomeRuns { get; set; }
        public int RunsBattedIn { get; set; }
        public int Runs { get; set; }
        public int Strikeouts { get; set; }
        public int Walks { get; set; }
        public int StolenBases { get; set; }

        // Pitching stats
        public int PitchingWins { get; set; }
        public int PitchingLosses { get; set; }
        public int GamesPitched { get; set; }
        public double Innings { get; set; } // e.g., 45.1 innings
        public int StrikeoutsPitching { get; set; }
        public int WalksPitching { get; set; }
        public int EarnedRuns { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual Player Player { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;

        // Computed properties
        public double BattingAverage => AtBats > 0 ? (double)Hits / AtBats : 0.000;
        public double OnBasePercentage => (Hits + Walks) > 0 ? (double)(Hits + Walks) / (AtBats + Walks) : 0.000;
        public double SluggingPercentage
        {
            get
            {
                if (AtBats == 0) return 0.000;
                int totalBases = Hits + Doubles + (2 * Triples) + (3 * HomeRuns);
                return (double)totalBases / AtBats;
            }
        }
        public double ERA => Innings > 0 ? (9.0 * EarnedRuns) / Innings : 0.00;
        public double WinPercentage => (PitchingWins + PitchingLosses) > 0 
            ? (double)PitchingWins / (PitchingWins + PitchingLosses) 
            : 0.000;
    }

    public class TeamStats
    {
        // Auto-increment primary key (internal use)
        public uint Id { get; set; }
        
        // External GUID identifier (unique)
        public string Guid { get; set; } = null!;
        
        public uint TeamId { get; set; }
        public int Season { get; set; }

        public int Wins { get; set; }
        public int Losses { get; set; }
        public int RunsFor { get; set; }
        public int RunsAgainst { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual Team Team { get; set; } = null!;

        // Computed properties
        public double WinPercentage => (Wins + Losses) > 0 
            ? (double)Wins / (Wins + Losses) 
            : 0.000;
        
        public int RunDifferential => RunsFor - RunsAgainst;
    }
}
