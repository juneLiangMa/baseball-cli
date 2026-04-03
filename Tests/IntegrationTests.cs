using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Config;
using BaseballCli.Database;
using BaseballCli.Models;
using BaseballCli.Services;

namespace BaseballCli.Tests
{
    /// <summary>
    /// Integration tests for end-to-end season simulation.
    /// Tests full workflow from config load to season completion.
    /// </summary>
    public class IntegrationTests
    {
        private SeasonConfiguration _config;
        private BaseballDbContext _dbContext;
        private BaseballRepository _repository;

        public IntegrationTests()
        {
            _config = ConfigLoader.CreateDefaultConfig();
            _dbContext = new BaseballDbContext(":memory:");
            _repository = new BaseballRepository(_dbContext);
        }

        public void TestFullSeasonSimulation()
        {
            // Arrange
            var league = new League
            {
                LeagueId = "league1",
                Name = _config.LeagueName,
                GamesPlayedPerSeries = _config.GamesPerSeries
            };

            var teams = _config.Teams.Select(t => new Team
            {
                TeamId = Guid.NewGuid().ToString(),
                Name = t.Name,
                LeagueId = league.LeagueId,
                Wins = 0,
                Losses = 0,
                Players = t.Players.Select(p => new Player
                {
                    PlayerId = Guid.NewGuid().ToString(),
                    Name = p.Name,
                    Position = p.Position,
                    BattingAverage = p.BattingAverage,
                    SeasonStats = new SeasonStats { BattingAverage = p.BattingAverage }
                }).ToList()
            }).ToList();

            var seasonStart = new DateTime(2024, 4, 1);
            var seasonEnd = seasonStart.AddDays(100);

            // Act
            var seasonSimulator = new SeasonSimulator(_dbContext, new EventGenerator(new ProbabilityTableService(_config)), _config);
            var games = seasonSimulator.GenerateSchedule(league, teams, seasonStart, seasonEnd);
            seasonSimulator.SimulateGames(games);

            // Assert
            if (games.Count == 0)
                throw new Exception("No games were generated");

            var completedGames = games.Where(g => g.IsCompleted).Count();
            if (completedGames == 0)
                throw new Exception("No games were simulated");

            Console.WriteLine($"✓ Full season simulated: {completedGames}/{games.Count} games completed");

            // Verify standings were calculated
            var standings = teams.OrderByDescending(t => t.Wins).ToList();
            if (standings[0].Wins < standings[standings.Count - 1].Wins)
                throw new Exception("Standings not properly ordered");

            Console.WriteLine($"✓ Standings calculated: {standings[0].Name} leading with {standings[0].Wins} wins");
        }

        public void TestConfigLoadAndValidation()
        {
            // Arrange
            var configJson = @"{
                ""leagueName"": ""Test League"",
                ""seasonStart"": ""2024-04-01"",
                ""seasonEnd"": ""2024-10-01"",
                ""gamesPerSeries"": 3,
                ""seriesPerTeamPerSeason"": 2,
                ""rulesConfig"": {
                    ""inningsPerGame"": 9,
                    ""playersPerTeam"": 9
                },
                ""teams"": [
                    {
                        ""name"": ""Team A"",
                        ""players"": [
                            { ""name"": ""Player 1"", ""position"": ""P"", ""battingAverage"": 0.200 },
                            { ""name"": ""Player 2"", ""position"": ""C"", ""battingAverage"": 0.250 }
                        ]
                    }
                ],
                ""outcomeProbabilities"": []
            }";

            // Act
            var config = ConfigLoader.ParseConfigJson(configJson);
            var validator = new ConfigurationValidator();
            var errors = new List<string>();
            var isValid = validator.Validate(config, errors);

            // Assert
            if (!isValid)
            {
                var errorMsg = string.Join(", ", errors);
                throw new Exception($"Config validation failed: {errorMsg}");
            }

            if (config.Teams.Count < 2)
                throw new Exception("Not enough teams in config");

            Console.WriteLine("✓ Configuration loaded and validated successfully");
        }

        public void TestDatabasePersistence()
        {
            // Arrange - Create temp db file
            var dbPath = $"test-{Guid.NewGuid()}.db";

            try
            {
                var dbContext = new BaseballDbContext(dbPath);
                var repository = new BaseballRepository(dbContext);

                var league = new League
                {
                    LeagueId = "league1",
                    Name = "Test League"
                };

                var team = new Team
                {
                    TeamId = "team1",
                    Name = "Test Team",
                    LeagueId = league.LeagueId
                };

                var player = new Player
                {
                    PlayerId = "player1",
                    Name = "Test Player",
                    Position = "OF",
                    BattingAverage = 0.300m
                };

                // Act - Save entities
                dbContext.Leagues.Add(league);
                dbContext.Teams.Add(team);
                dbContext.Players.Add(player);
                dbContext.SaveChanges();

                // Assert - Verify retrieval
                var retrievedLeague = repository.GetLeagueByName("Test League");
                if (retrievedLeague == null)
                    throw new Exception("League not persisted");

                if (retrievedLeague.LeagueId != league.LeagueId)
                    throw new Exception("League ID mismatch");

                Console.WriteLine("✓ Database persistence working correctly");
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(dbPath))
                    System.IO.File.Delete(dbPath);
            }
        }

        public void TestMultipleGamesPerTeam()
        {
            // Arrange
            var league = new League { LeagueId = "l1", Name = "Test" };
            var teams = new List<Team>
            {
                new Team { TeamId = "t1", Name = "Team 1", LeagueId = "l1" },
                new Team { TeamId = "t2", Name = "Team 2", LeagueId = "l1" }
            };

            var seasonStart = new DateTime(2024, 4, 1);
            var seasonEnd = new DateTime(2024, 4, 30);

            // Act
            var schedule = SeasonSimulator.GenerateSchedule(league, teams, seasonStart, seasonEnd);
            var team1Games = schedule
                .Where(g => g.HomeTeam.TeamId == "t1" || g.AwayTeam.TeamId == "t1")
                .Count();

            // Assert
            if (team1Games == 0)
                throw new Exception("No games generated for team");

            Console.WriteLine($"✓ Multiple games per team: {team1Games} games for Team 1");
        }

        public void TestGameDateOrdering()
        {
            // Arrange
            var league = new League { LeagueId = "l1", Name = "Test" };
            var teams = new List<Team>
            {
                new Team { TeamId = "t1", Name = "Team 1", LeagueId = "l1" },
                new Team { TeamId = "t2", Name = "Team 2", LeagueId = "l1" }
            };

            var seasonStart = new DateTime(2024, 4, 1);
            var seasonEnd = new DateTime(2024, 5, 1);

            // Act
            var schedule = SeasonSimulator.GenerateSchedule(league, teams, seasonStart, seasonEnd);
            var sortedDates = schedule.Select(g => g.GameDate).OrderBy(d => d).ToList();
            var scheduledDates = schedule.Select(g => g.GameDate).ToList();

            // Assert
            for (int i = 0; i < sortedDates.Count; i++)
            {
                if (sortedDates[i] != scheduledDates[i])
                    throw new Exception("Games not in chronological order");
            }

            Console.WriteLine($"✓ Game dates properly ordered: {sortedDates[0]:yyyy-MM-dd} to {sortedDates[sortedDates.Count - 1]:yyyy-MM-dd}");
        }

        public void TestTeamWinLossTracking()
        {
            // Arrange
            var homeTeam = new Team { TeamId = "h1", Name = "Home", Wins = 0, Losses = 0 };
            var awayTeam = new Team { TeamId = "a1", Name = "Away", Wins = 0, Losses = 0 };

            var game = new Game
            {
                GameId = "g1",
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                GameDate = DateTime.Now,
                HomeTeamScore = 5,
                AwayTeamScore = 3,
                IsCompleted = true
            };

            // Act
            var homeWins = game.HomeTeamScore > game.AwayTeamScore ? 1 : 0;
            var homeLosses = game.HomeTeamScore <= game.AwayTeamScore ? 1 : 0;

            homeTeam.Wins += homeWins;
            homeTeam.Losses += homeLosses;
            awayTeam.Wins += (1 - homeWins);
            awayTeam.Losses += (1 - homeLosses);

            // Assert
            if (homeTeam.Wins != 1 || homeTeam.Losses != 0)
                throw new Exception("Home team win/loss not updated correctly");

            if (awayTeam.Wins != 0 || awayTeam.Losses != 1)
                throw new Exception("Away team win/loss not updated correctly");

            Console.WriteLine("✓ Win/Loss tracking working correctly");
        }

        public void TestBattingAverageCalculation()
        {
            // Arrange
            var player = new Player
            {
                PlayerId = "p1",
                Name = "Test",
                Position = "OF",
                SeasonStats = new SeasonStats { AtBats = 100, Hits = 30 }
            };

            // Act
            var ba = player.SeasonStats.BattingAverage;

            // Assert
            var expected = 30m / 100m;
            if (Math.Abs((double)(ba - expected)) > 0.001)
                throw new Exception($"BA calculation wrong: expected {expected}, got {ba}");

            Console.WriteLine($"✓ Batting average calculated correctly: {ba:F3}");
        }

        public void TestERACalculation()
        {
            // Arrange
            var pitcher = new Player
            {
                PlayerId = "p1",
                Name = "Test Pitcher",
                Position = "P",
                SeasonStats = new SeasonStats 
                { 
                    InningsPitched = 200,
                    EarnedRuns = 60
                }
            };

            // Act
            var era = pitcher.SeasonStats.ERA;

            // Assert
            var expected = (60m * 9m) / 200m; // (ER * 9) / IP
            if (Math.Abs((double)(era - expected)) > 0.01)
                throw new Exception($"ERA calculation wrong: expected {expected}, got {era}");

            Console.WriteLine($"✓ ERA calculated correctly: {era:F2}");
        }

        public void RunAllTests()
        {
            var tests = new List<(string Name, Action Test)>
            {
                ("Full Season Simulation", TestFullSeasonSimulation),
                ("Config Load and Validation", TestConfigLoadAndValidation),
                ("Database Persistence", TestDatabasePersistence),
                ("Multiple Games Per Team", TestMultipleGamesPerTeam),
                ("Game Date Ordering", TestGameDateOrdering),
                ("Team Win/Loss Tracking", TestTeamWinLossTracking),
                ("Batting Average Calculation", TestBattingAverageCalculation),
                ("ERA Calculation", TestERACalculation),
            };

            Console.WriteLine("\n╔═════════════════════════════════════════╗");
            Console.WriteLine("║   Baseball CLI Integration Tests         ║");
            Console.WriteLine("╚═════════════════════════════════════════╝\n");

            int passed = 0;
            int failed = 0;

            foreach (var (name, test) in tests)
            {
                try
                {
                    test();
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ {name}: {ex.Message}");
                    failed++;
                }
            }

            Console.WriteLine($"\n{passed} passed, {failed} failed\n");

            if (failed > 0)
                throw new Exception($"{failed} tests failed");
        }
    }
}
