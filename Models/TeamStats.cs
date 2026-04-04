using System;

namespace BaseballCli.Models
{
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
