using System;
using System.Collections.Generic;

namespace BaseballCli.Models
{
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
}
