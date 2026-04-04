using System;

namespace BaseballCli.Models
{
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
}
