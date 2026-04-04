using Microsoft.EntityFrameworkCore;
using BaseballCli.Models;
using BaseballCli.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BaseballCli.Database
{
    public class BaseballDbContext : DbContext
    {
        private readonly string _connectionString;

        public BaseballDbContext(string dbPath = "baseball.db")
        {
            _connectionString = $"Data Source={dbPath};";
        }

        public DbSet<League> Leagues { get; set; } = null!;
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<Play> Plays { get; set; } = null!;
        public DbSet<SeasonStats> SeasonStats { get; set; } = null!;
        public DbSet<TeamStats> TeamStats { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // League configuration
            modelBuilder.Entity<League>()
                .HasKey(l => l.Id);
            modelBuilder.Entity<League>()
                .Property(l => l.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<League>()
                .HasIndex(l => l.Guid)
                .IsUnique();
            modelBuilder.Entity<League>()
                .HasIndex(l => l.Name)
                .IsUnique();
            modelBuilder.Entity<League>()
                .HasMany(l => l.Teams)
                .WithOne(t => t.League)
                .HasForeignKey(t => t.LeagueId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<League>()
                .HasMany(l => l.Games)
                .WithOne(g => g.League)
                .HasForeignKey(g => g.LeagueId)
                .OnDelete(DeleteBehavior.Cascade);

            // Team configuration
            modelBuilder.Entity<Team>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<Team>()
                .Property(t => t.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Guid)
                .IsUnique();
            modelBuilder.Entity<Team>()
                .HasIndex(t => new { t.LeagueId, t.Name })
                .IsUnique();
            modelBuilder.Entity<Team>()
                .HasMany(t => t.Players)
                .WithOne(p => p.Team)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Team>()
                .HasMany(t => t.HomeGames)
                .WithOne(g => g.HomeTeam)
                .HasForeignKey(g => g.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Team>()
                .HasMany(t => t.AwayGames)
                .WithOne(g => g.AwayTeam)
                .HasForeignKey(g => g.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Team>()
                .HasMany(t => t.SeasonStats)
                .WithOne(s => s.Team)
                .HasForeignKey(s => s.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Player configuration
            modelBuilder.Entity<Player>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<Player>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Guid)
                .IsUnique();
            modelBuilder.Entity<Player>()
                .HasIndex(p => new { p.TeamId, p.Name })
                .IsUnique();
            modelBuilder.Entity<Player>()
                .HasMany(p => p.SeasonStats)
                .WithOne(s => s.Player)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Game configuration
            modelBuilder.Entity<Game>()
                .HasKey(g => g.Id);
            modelBuilder.Entity<Game>()
                .Property(g => g.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Game>()
                .HasIndex(g => g.Guid)
                .IsUnique();
            modelBuilder.Entity<Game>()
                .HasIndex(g => new { g.LeagueId, g.GameDate });
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Plays)
                .WithOne(p => p.Game)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Play configuration
            modelBuilder.Entity<Play>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<Play>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Play>()
                .HasIndex(p => p.Guid)
                .IsUnique();
            modelBuilder.Entity<Play>()
                .HasOne(p => p.Batter)
                .WithMany()
                .HasForeignKey(p => p.BatterId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Play>()
                .HasOne(p => p.Pitcher)
                .WithMany()
                .HasForeignKey(p => p.PitcherId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Play>()
                .HasOne(p => p.BatterTeam)
                .WithMany()
                .HasForeignKey(p => p.BatterTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // SeasonStats configuration
            modelBuilder.Entity<SeasonStats>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<SeasonStats>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<SeasonStats>()
                .HasIndex(s => s.Guid)
                .IsUnique();
            modelBuilder.Entity<SeasonStats>()
                .HasIndex(s => new { s.PlayerId, s.Season })
                .IsUnique();

            // TeamStats configuration
            modelBuilder.Entity<TeamStats>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<TeamStats>()
                .Property(t => t.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<TeamStats>()
                .HasIndex(t => t.Guid)
                .IsUnique();
            modelBuilder.Entity<TeamStats>()
                .HasIndex(t => new { t.TeamId, t.Season })
                .IsUnique();
        }

        /// <summary>
        /// Initialize database schema if it doesn't exist.
        /// </summary>
        public void InitializeDatabase()
        {
            Database.EnsureCreated();
        }

        /// <summary>
        /// Delete and recreate the database (for testing).
        /// </summary>
        public void ResetDatabase()
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        // Query methods for various entities
        public List<Game> GetGamesByLeague(uint leagueId)
        {
            return Games
                .Where(g => g.LeagueId == leagueId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Include(g => g.Plays)
                .OrderBy(g => g.GameDate)
                .ToList();
        }

        public List<Team> GetTeamsByLeague(uint leagueId)
        {
            return Teams
                .Where(t => t.LeagueId == leagueId)
                .Include(t => t.Players)
                .OrderBy(t => t.Name)
                .ToList();
        }

        public List<Player> GetPlayersByTeam(uint teamId)
        {
            return Players
                .Where(p => p.TeamId == teamId)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public List<Play> GetPlaysByPlayer(uint playerId)
        {
            return Plays
                .Where(p => p.BatterId == playerId || p.PitcherId == playerId)
                .Include(p => p.Game)
                .Include(p => p.Batter)
                .Include(p => p.Pitcher)
                .OrderBy(p => p.Game.GameDate)
                .ToList();
        }
    }

    /// <summary>
    /// Repository pattern implementation for data access.
    /// </summary>
    public class BaseballRepository
    {
        private readonly BaseballDbContext _context;

        public BaseballRepository(BaseballDbContext context)
        {
            _context = context;
        }

        // League operations
        public void AddLeague(League league)
        {
            _context.Leagues.Add(league);
            _context.SaveChanges();
        }

        public League? GetLeague(uint leagueId)
        {
            return _context.Leagues
                .Include(l => l.Teams)
                .ThenInclude(t => t.Players)
                .FirstOrDefault(l => l.Id == leagueId);
        }

        public League? GetLeagueByName(string name)
        {
            return _context.Leagues
                .FirstOrDefault(l => l.Name == name);
        }

        public List<League> GetAllLeagues()
        {
            return _context.Leagues.ToList();
        }

        // Team operations
        public void AddTeam(Team team)
        {
            _context.Teams.Add(team);
            _context.SaveChanges();
        }

        public Team? GetTeam(uint teamId)
        {
            return _context.Teams
                .Include(t => t.Players)
                .Include(t => t.SeasonStats)
                .FirstOrDefault(t => t.Id == teamId);
        }

        public List<Team> GetTeamsByLeague(uint leagueId)
        {
            return _context.Teams
                .Where(t => t.LeagueId == leagueId)
                .Include(t => t.Players)
                .ToList();
        }

        // Player operations
        public void AddPlayer(Player player)
        {
            _context.Players.Add(player);
            _context.SaveChanges();
        }

        public Player? GetPlayer(uint playerId)
        {
            return _context.Players
                .Include(p => p.SeasonStats)
                .FirstOrDefault(p => p.Id == playerId);
        }

        public List<Player> GetPlayersByTeam(uint teamId)
        {
            return _context.Players
                .Where(p => p.TeamId == teamId)
                .ToList();
        }

        // Game operations
        public void AddGame(Game game)
        {
            _context.Games.Add(game);
            _context.SaveChanges();
        }

        public Game? GetGame(uint gameId)
        {
            return _context.Games
                .Include(g => g.Plays)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .FirstOrDefault(g => g.Id == gameId);
        }

        public List<Game> GetGamesByDate(DateTime date, uint leagueId)
        {
            return _context.Games
                .Where(g => g.GameDate.Date == date.Date && g.LeagueId == leagueId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .ToList();
        }

        public List<Game> GetGamesByTeamAndDateRange(uint teamId, DateTime startDate, DateTime endDate)
        {
            return _context.Games
                .Where(g => (g.HomeTeamId == teamId || g.AwayTeamId == teamId) &&
                           g.GameDate >= startDate && g.GameDate <= endDate)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .OrderBy(g => g.GameDate)
                .ToList();
        }

        // Play operations
        public void AddPlay(Play play)
        {
            _context.Plays.Add(play);
            _context.SaveChanges();
        }

        public List<Play> GetPlaysByGame(uint gameId)
        {
            return _context.Plays
                .Where(p => p.GameId == gameId)
                .Include(p => p.Batter)
                .Include(p => p.Pitcher)
                .OrderBy(p => p.PlayNumber)
                .ToList();
        }

        // SeasonStats operations
        public void AddOrUpdateSeasonStats(SeasonStats stats)
        {
            var existing = _context.SeasonStats
                .FirstOrDefault(s => s.PlayerId == stats.PlayerId && s.Season == stats.Season);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(stats);
            }
            else
            {
                _context.SeasonStats.Add(stats);
            }

            _context.SaveChanges();
        }

        /// <summary>
        /// Get existing season stats or create new if they don't exist.
        /// </summary>
        public SeasonStats GetOrCreateSeasonStats(uint playerId, uint teamId, int season)
        {
            var existing = GetSeasonStats(playerId, season);
            if (existing != null)
                return existing;

            var stats = new SeasonStats
            {
                PlayerId = playerId,
                TeamId = teamId,
                Season = season,
                UpdatedAt = DateTime.UtcNow,
                Guid = System.Guid.NewGuid().ToString()
            };
            _context.SeasonStats.Add(stats);
            _context.SaveChanges();
            return stats;
        }

        public SeasonStats? GetSeasonStats(uint playerId, int season)
        {
            return _context.SeasonStats
                .FirstOrDefault(s => s.PlayerId == playerId && s.Season == season);
        }

        public List<SeasonStats> GetSeasonStatsByTeam(uint teamId, int season)
        {
            return _context.SeasonStats
                .Where(s => s.TeamId == teamId && s.Season == season)
                .Include(s => s.Player)
                .OrderByDescending(s => s.BattingAverage)
                .ToList();
        }

        // TeamStats operations
        public void AddOrUpdateTeamStats(TeamStats stats)
        {
            var existing = _context.TeamStats
                .FirstOrDefault(s => s.TeamId == stats.TeamId && s.Season == stats.Season);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(stats);
            }
            else
            {
                _context.TeamStats.Add(stats);
            }

            _context.SaveChanges();
        }

        public TeamStats? GetTeamStats(uint teamId, int season)
        {
            return _context.TeamStats
                .FirstOrDefault(s => s.TeamId == teamId && s.Season == season);
        }

        public List<TeamStats> GetLeagueStandings(uint leagueId, int season)
        {
            var teamIds = _context.Teams
                .Where(t => t.LeagueId == leagueId)
                .Select(t => t.Id)
                .ToList();

            return _context.TeamStats
                .Where(s => teamIds.Contains(s.TeamId) && s.Season == season)
                .Include(s => s.Team)
                .OrderByDescending(s => s.Wins)
                .ThenBy(s => s.Losses)
                .ThenByDescending(s => s.WinPercentage)
                .ToList();
        }

        public List<Game> GetGamesByLeague(uint leagueId)
        {
            return _context.Games
                .Where(g => g.LeagueId == leagueId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Include(g => g.Plays)
                .OrderBy(g => g.GameDate)
                .ToList();
        }

        public List<Play> GetPlaysByPlayer(uint playerId)
        {
            return _context.Plays
                .Where(p => p.BatterId == playerId || p.PitcherId == playerId)
                .Include(p => p.Game)
                .Include(p => p.Batter)
                .Include(p => p.Pitcher)
                .OrderBy(p => p.Game.GameDate)
                .ToList();
        }

        public Player? GetPlayerByName(string name)
        {
            return _context.Players.FirstOrDefault(p => p.Name == name);
        }

        public List<Game> GetGamesByTeam(uint teamId)
        {
            return _context.Games
                .Where(g => g.HomeTeamId == teamId || g.AwayTeamId == teamId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .OrderBy(g => g.GameDate)
                .ToList();
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        /// <summary>
        /// Load a complete season configuration into the database (all leagues, teams, players).
        /// </summary>
        public void LoadSeasonConfiguration(SeasonConfiguration config)
        {
            // Create league
            var league = new League
            {
                Guid = System.Guid.NewGuid().ToString(),
                Name = config.League.Name,
                CreatedAt = DateTime.UtcNow
            };
            _context.Leagues.Add(league);
            _context.SaveChanges();

            // Create teams and players
            foreach (var teamConfig in config.Teams)
            {
                var team = new Team
                {
                    LeagueId = league.Id,
                    Guid = System.Guid.NewGuid().ToString(),
                    Name = teamConfig.Name,
                    City = teamConfig.City,
                    ManagerName = teamConfig.Manager,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Teams.Add(team);
                _context.SaveChanges();

                // Add players to team
                if (teamConfig.Players != null)
                {
                    foreach (var playerConfig in teamConfig.Players)
                    {
                        var player = new Player
                        {
                            TeamId = team.Id,
                            Guid = System.Guid.NewGuid().ToString(),
                            Name = playerConfig.Name,
                            Gender = playerConfig.Gender,
                            Position = playerConfig.Position,
                            BattingAverage = playerConfig.BattingAverage,
                            PowerRating = playerConfig.PowerRating,
                            SpeedRating = playerConfig.SpeedRating,
                            FieldingAverage = playerConfig.FieldingAverage,
                            PitchingSpeed = playerConfig.PitchingSpeed,
                            ControlRating = playerConfig.ControlRating,
                            Salary = playerConfig.Salary,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Players.Add(player);
                    }
                }
                _context.SaveChanges();
            }
        }
    }
}
