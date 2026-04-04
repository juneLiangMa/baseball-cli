using System;
using System.Collections.Generic;

namespace BaseballCli.Models
{
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
}
