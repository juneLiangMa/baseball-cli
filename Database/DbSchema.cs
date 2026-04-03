using System;

namespace BaseballCli.Database
{
    /// <summary>
    /// Initialization script for the Baseball CLI database schema.
    /// This creates the core tables and relationships for seasons, teams, players, games, and stats.
    /// </summary>
    public static class DbSchema
    {
        public static readonly string[] InitializationScripts = new[]
        {
            // Leagues table
            @"
            CREATE TABLE IF NOT EXISTS Leagues (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            ",

            // Teams table
            @"
            CREATE TABLE IF NOT EXISTS Teams (
                Id TEXT PRIMARY KEY,
                LeagueId TEXT NOT NULL,
                Name TEXT NOT NULL,
                City TEXT NOT NULL,
                ManagerName TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (LeagueId) REFERENCES Leagues(Id) ON DELETE CASCADE,
                UNIQUE(LeagueId, Name)
            );
            ",

            // Players table (includes both batting and pitching stats baseline)
            @"
            CREATE TABLE IF NOT EXISTS Players (
                Id TEXT PRIMARY KEY,
                TeamId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Gender TEXT NOT NULL,
                Position TEXT NOT NULL,
                BattingAverage REAL NOT NULL DEFAULT 0.250,
                PowerRating REAL NOT NULL DEFAULT 0.400,
                SpeedRating REAL NOT NULL DEFAULT 0.400,
                FieldingAverage REAL NOT NULL DEFAULT 0.950,
                PitchingSpeed REAL,
                ControlRating REAL,
                Salary REAL NOT NULL DEFAULT 1000000,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
                UNIQUE(TeamId, Name)
            );
            ",

            // Games table
            @"
            CREATE TABLE IF NOT EXISTS Games (
                Id TEXT PRIMARY KEY,
                LeagueId TEXT NOT NULL,
                HomeTeamId TEXT NOT NULL,
                AwayTeamId TEXT NOT NULL,
                GameDate DATE NOT NULL,
                Season INT NOT NULL,
                HomeScore INT NOT NULL DEFAULT 0,
                AwayScore INT NOT NULL DEFAULT 0,
                Status TEXT NOT NULL DEFAULT 'NotStarted',
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (LeagueId) REFERENCES Leagues(Id) ON DELETE CASCADE,
                FOREIGN KEY (HomeTeamId) REFERENCES Teams(Id),
                FOREIGN KEY (AwayTeamId) REFERENCES Teams(Id)
            );
            ",

            // Plays/Plays table (individual at-bats and events)
            @"
            CREATE TABLE IF NOT EXISTS Plays (
                Id TEXT PRIMARY KEY,
                GameId TEXT NOT NULL,
                Inning INT NOT NULL,
                PlayNumber INT NOT NULL,
                BatterId TEXT NOT NULL,
                PitcherId TEXT NOT NULL,
                EventType TEXT NOT NULL,
                Result TEXT NOT NULL,
                BatterTeamId TEXT NOT NULL,
                Outs INT DEFAULT 0,
                RunnersOnBase TEXT,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (GameId) REFERENCES Games(Id) ON DELETE CASCADE,
                FOREIGN KEY (BatterId) REFERENCES Players(Id),
                FOREIGN KEY (PitcherId) REFERENCES Players(Id),
                FOREIGN KEY (BatterTeamId) REFERENCES Teams(Id)
            );
            ",

            // Season Stats table (aggregated player stats per season)
            @"
            CREATE TABLE IF NOT EXISTS SeasonStats (
                Id TEXT PRIMARY KEY,
                PlayerId TEXT NOT NULL,
                TeamId TEXT NOT NULL,
                Season INT NOT NULL,
                GamesPlayed INT DEFAULT 0,
                AtBats INT DEFAULT 0,
                Hits INT DEFAULT 0,
                Doubles INT DEFAULT 0,
                Triples INT DEFAULT 0,
                HomeRuns INT DEFAULT 0,
                RunsBattedIn INT DEFAULT 0,
                Runs INT DEFAULT 0,
                Strikeouts INT DEFAULT 0,
                Walks INT DEFAULT 0,
                StolenBases INT DEFAULT 0,
                BattingAverage REAL DEFAULT 0.000,
                OnBasePercentage REAL DEFAULT 0.000,
                SlugsingPercentage REAL DEFAULT 0.000,
                PitchingWins INT DEFAULT 0,
                PitchingLosses INT DEFAULT 0,
                GamesPitched INT DEFAULT 0,
                Innings REAL DEFAULT 0.0,
                Strikeouts_Pitching INT DEFAULT 0,
                Walks_Pitching INT DEFAULT 0,
                EarnedRuns INT DEFAULT 0,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (PlayerId) REFERENCES Players(Id) ON DELETE CASCADE,
                FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
                UNIQUE(PlayerId, Season)
            );
            ",

            // Team Stats table (aggregated team stats per season)
            @"
            CREATE TABLE IF NOT EXISTS TeamStats (
                Id TEXT PRIMARY KEY,
                TeamId TEXT NOT NULL,
                Season INT NOT NULL,
                Wins INT DEFAULT 0,
                Losses INT DEFAULT 0,
                RunsFor INT DEFAULT 0,
                RunsAgainst INT DEFAULT 0,
                UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE,
                UNIQUE(TeamId, Season)
            );
            ",

            // Create indexes for common queries
            @"
            CREATE INDEX IF NOT EXISTS idx_teams_league ON Teams(LeagueId);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_players_team ON Players(TeamId);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_games_league ON Games(LeagueId);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_games_date ON Games(GameDate);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_plays_game ON Plays(GameId);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_season_stats_player ON SeasonStats(PlayerId);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_season_stats_team ON SeasonStats(TeamId);
            ",

            @"
            CREATE INDEX IF NOT EXISTS idx_team_stats_team ON TeamStats(TeamId);
            "
        };
    }
}
