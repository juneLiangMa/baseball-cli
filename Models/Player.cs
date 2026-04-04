using System;
using System.Collections.Generic;

namespace BaseballCli.Models
{
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
}
