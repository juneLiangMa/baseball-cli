using System;

namespace BaseballCli.Models
{
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
}
