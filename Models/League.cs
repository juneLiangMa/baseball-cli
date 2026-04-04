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
}
