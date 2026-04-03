using Microsoft.EntityFrameworkCore;
using BaseballCli.Models;
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
                .HasIndex(l => l.Name)
                .IsUnique();
            modelBuilder.Entity<League>()
                .HasMany(l => l.Teams)
                .WithOne(t => t.League)
                .HasForeignKey(t => t.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Team configuration
            modelBuilder.Entity<Team>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<Team>()
                .HasIndex(t => new { t.Id, t.Name })
                .IsUnique();
            modelBuilder.Entity<Team>()
                .HasMany(t => t.Players)
                .WithOne(p => p.Team)
                .HasForeignKey(p => p.Id)
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

            // Player configuration
            modelBuilder.Entity<Player>()
                .HasKey(p => p.Id);
            modelBuilder.Entity<Player>()
                .HasIndex(p => new { p.Id, p.Name })
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
                .HasIndex(g => g.GameDate);
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Plays)
                .WithOne(p => p.Game)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Play configuration
            modelBuilder.Entity<Play>()
                .HasKey(p => p.Id);
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

            // SeasonStats configuration
            modelBuilder.Entity<SeasonStats>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<SeasonStats>()
                .HasIndex(s => new { s.PlayerId, s.Season })
                .IsUnique();

            // TeamStats configuration
            modelBuilder.Entity<TeamStats>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<TeamStats>()
                .HasIndex(t => new { t.Id, t.Season })
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
        public List<Game> GetGamesByLeague(string leagueId)
        {
            return Games
                .Where(g => g.Id == leagueId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Include(g => g.Plays)
                .OrderBy(g => g.GameDate)
                .ToList();
        }

        public List<Team> GetTeamsByLeague(string leagueId)
        {
            return Teams
                .Where(t => t.Id == leagueId)
                .Include(t => t.Players)
                .OrderBy(t => t.Name)
                .ToList();
        }

        public List<Player> GetPlayersByTeam(string teamId)
        {
            return Players
                .Where(p => p.Id == teamId)
                .OrderBy(p => p.Name)
                .ToList();
        }

        public List<Play> GetPlaysByPlayer(string playerId)
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

        public League? GetLeague(string leagueId)
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

        public Team? GetTeam(string teamId)
        {
            return _context.Teams
                .Include(t => t.Players)
                .Include(t => t.SeasonStats)
                .FirstOrDefault(t => t.Id == teamId);
        }

        public List<Team> GetTeamsByLeague(string leagueId)
        {
            return _context.Teams
                .Where(t => t.Id == leagueId)
                .Include(t => t.Players)
                .ToList();
        }

        // Player operations
        public void AddPlayer(Player player)
        {
            _context.Players.Add(player);
            _context.SaveChanges();
        }

        public Player? GetPlayer(string playerId)
        {
            return _context.Players
                .Include(p => p.SeasonStats)
                .FirstOrDefault(p => p.Id == playerId);
        }

        public List<Player> GetPlayersByTeam(string teamId)
        {
            return _context.Players
                .Where(p => p.Id == teamId)
                .ToList();
        }

        // Game operations
        public void AddGame(Game game)
        {
            _context.Games.Add(game);
            _context.SaveChanges();
        }

        public Game? GetGame(string gameId)
        {
            return _context.Games
                .Include(g => g.Plays)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .FirstOrDefault(g => g.Id == gameId);
        }

        public List<Game> GetGamesByDate(DateTime date, string leagueId)
        {
            return _context.Games
                .Where(g => g.GameDate.Date == date.Date && g.Id == leagueId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .ToList();
        }

        public List<Game> GetGamesByTeamAndDateRange(string teamId, DateTime startDate, DateTime endDate)
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

        public List<Play> GetPlaysByGame(string gameId)
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

        public SeasonStats? GetSeasonStats(string playerId, int season)
        {
            return _context.SeasonStats
                .FirstOrDefault(s => s.PlayerId == playerId && s.Season == season);
        }

        public List<SeasonStats> GetSeasonStatsByTeam(string teamId, int season)
        {
            return _context.SeasonStats
                .Where(s => s.Id == teamId && s.Season == season)
                .Include(s => s.Player)
                .OrderByDescending(s => s.BattingAverage)
                .ToList();
        }

        // TeamStats operations
        public void AddOrUpdateTeamStats(TeamStats stats)
        {
            var existing = _context.TeamStats
                .FirstOrDefault(s => s.Id == stats.Id && s.Season == stats.Season);

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

        public TeamStats? GetTeamStats(string teamId, int season)
        {
            return _context.TeamStats
                .FirstOrDefault(s => s.Id == teamId && s.Season == season);
        }

        public List<TeamStats> GetLeagueStandings(string leagueId, int season)
        {
            var teamIds = _context.Teams
                .Where(t => t.Id == leagueId)
                .Select(t => t.Id)
                .ToList();

            return _context.TeamStats
                .Where(s => teamIds.Contains(s.Id) && s.Season == season)
                .Include(s => s.Team)
                .OrderByDescending(s => s.Wins)
                .ThenBy(s => s.Losses)
                .ThenByDescending(s => s.WinPercentage)
                .ToList();
        }

        public List<Game> GetGamesByLeague(string leagueId)
        {
            return _context.Games
                .Where(g => g.Id == leagueId)
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Include(g => g.Plays)
                .OrderBy(g => g.GameDate)
                .ToList();
        }

        public List<Play> GetPlaysByPlayer(string playerId)
        {
            return _context.Plays
                .Where(p => p.BatterId == playerId || p.PitcherId == playerId)
                .Include(p => p.Game)
                .Include(p => p.Batter)
                .Include(p => p.Pitcher)
                .OrderBy(p => p.Game.GameDate)
                .ToList();
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
