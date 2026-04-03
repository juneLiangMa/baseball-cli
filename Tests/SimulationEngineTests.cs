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
    /// Unit tests for core simulation engine components.
    /// Tests probability resolution, event generation, and game simulation logic.
    /// </summary>
    public class SimulationEngineTests
    {
        private SeasonConfiguration _config;
        private ProbabilityTableService _probabilityService;
        private EventGenerator _eventGenerator;
        private Random _testRandom;

        public SimulationEngineTests()
        {
            _testRandom = new Random(42); // Fixed seed for deterministic tests
            _config = ConfigLoader.CreateDefaultConfig();
            _probabilityService = new ProbabilityTableService(_config);
            _eventGenerator = new EventGenerator(_probabilityService);
        }

        public void TestProbabilityTableLoadsCorrectly()
        {
            // Arrange & Act
            var table = _config.OutcomeProbabilities;

            // Assert
            if (table == null || table.Length == 0)
                throw new Exception("Probability tables not loaded");

            foreach (var entry in table)
            {
                if (entry.ProbabilityMultiplier <= 0 || entry.ProbabilityMultiplier > 5)
                    throw new Exception($"Invalid probability multiplier: {entry.ProbabilityMultiplier}");

                if (string.IsNullOrEmpty(entry.BatterHandedness) || 
                    string.IsNullOrEmpty(entry.PitcherHandedness) ||
                    string.IsNullOrEmpty(entry.PitchType))
                    throw new Exception("Probability table has invalid entries");
            }

            Console.WriteLine("✓ Probability tables loaded correctly");
        }

        public void TestEventGenerationProducesValidResults()
        {
            // Arrange
            var batter = new Player 
            { 
                PlayerId = "bat1", 
                Name = "Test Batter", 
                Position = "C",
                BattingAverage = 0.300m,
                SeasonStats = new SeasonStats { BattingAverage = 0.300m }
            };

            var pitcher = new Player 
            { 
                PlayerId = "pit1", 
                Name = "Test Pitcher", 
                Position = "P",
                SeasonStats = new SeasonStats { ERA = 3.50m }
            };

            // Act
            var result = _eventGenerator.GenerateAtBatResult(batter, pitcher, "R", "R", new Dictionary<string, object>());

            // Assert
            if (string.IsNullOrEmpty(result.EventType))
                throw new Exception("Event generation produced null result");

            var validEvents = new[] { "Hit", "Walk", "Strikeout", "Out", "HomeRun", "Double", "Triple", "Error" };
            if (!validEvents.Contains(result.EventType))
                throw new Exception($"Invalid event type generated: {result.EventType}");

            Console.WriteLine($"✓ Event generation produced valid result: {result.EventType}");
        }

        public void TestBatterSkillAffectsProbability()
        {
            // Arrange
            var goodBatter = new Player 
            { 
                PlayerId = "good", 
                Name = "Good Batter", 
                Position = "OF",
                BattingAverage = 0.350m,
                SeasonStats = new SeasonStats { BattingAverage = 0.350m }
            };

            var poorBatter = new Player 
            { 
                PlayerId = "poor", 
                Name = "Poor Batter", 
                Position = "OF",
                BattingAverage = 0.200m,
                SeasonStats = new SeasonStats { BattingAverage = 0.200m }
            };

            var pitcher = new Player 
            { 
                PlayerId = "pit1", 
                Name = "Average Pitcher", 
                Position = "P",
                SeasonStats = new SeasonStats { ERA = 3.50m }
            };

            // Act - Generate multiple at-bats and count hits
            var goodBatterHits = 0;
            var poorBatterHits = 0;
            const int trials = 100;

            for (int i = 0; i < trials; i++)
            {
                var goodResult = _eventGenerator.GenerateAtBatResult(goodBatter, pitcher, "R", "R", new Dictionary<string, object>());
                if (new[] { "Hit", "Double", "Triple", "HomeRun" }.Contains(goodResult.EventType))
                    goodBatterHits++;

                var poorResult = _eventGenerator.GenerateAtBatResult(poorBatter, pitcher, "R", "R", new Dictionary<string, object>());
                if (new[] { "Hit", "Double", "Triple", "HomeRun" }.Contains(poorResult.EventType))
                    poorBatterHits++;
            }

            // Assert - Good batter should get more hits
            if (goodBatterHits <= poorBatterHits)
                throw new Exception($"Good batter ({goodBatterHits} hits) didn't outperform poor batter ({poorBatterHits} hits)");

            Console.WriteLine($"✓ Skill affects probability: Good batter {goodBatterHits} hits, Poor batter {poorBatterHits} hits");
        }

        public void TestConfigurationValidation()
        {
            // Arrange
            var config = ConfigLoader.CreateDefaultConfig();

            // Act
            var errors = new List<string>();
            var validator = new ConfigurationValidator();
            var isValid = validator.Validate(config, errors);

            // Assert
            if (!isValid)
            {
                var errorMsg = string.Join(", ", errors);
                throw new Exception($"Default config failed validation: {errorMsg}");
            }

            Console.WriteLine("✓ Configuration validation passed");
        }

        public void TestGameSimulatorCompletesGame()
        {
            // Arrange
            var dbContext = new BaseballDbContext(":memory:");
            var simulator = new GameSimulator(dbContext, _eventGenerator, _config);

            var homeTeam = new Team 
            { 
                TeamId = "home", 
                Name = "Home Team",
                Wins = 0,
                Losses = 0
            };

            var awayTeam = new Team 
            { 
                TeamId = "away", 
                Name = "Away Team",
                Wins = 0,
                Losses = 0
            };

            var game = new Game
            {
                GameId = "game1",
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                GameDate = DateTime.Now,
                HomeTeamScore = 0,
                AwayTeamScore = 0,
                IsCompleted = false
            };

            // Create minimal player rosters
            homeTeam.Players = CreateMinimalRoster("H");
            awayTeam.Players = CreateMinimalRoster("A");

            // Act
            simulator.SimulateGame(game);

            // Assert
            if (!game.IsCompleted)
                throw new Exception("Game not marked as completed");

            if (game.HomeTeamScore < 0 || game.AwayTeamScore < 0)
                throw new Exception("Negative score generated");

            if (game.HomeTeamScore > 100 || game.AwayTeamScore > 100)
                throw new Exception("Unrealistic score generated");

            Console.WriteLine($"✓ Game simulated successfully: {awayTeam.Name} {game.AwayTeamScore}, {homeTeam.Name} {game.HomeTeamScore}");
        }

        public void TestSeasonScheduleGeneration()
        {
            // Arrange
            var league = new League { LeagueId = "l1", Name = "Test League" };
            var teams = new List<Team>
            {
                new Team { TeamId = "t1", Name = "Team 1", LeagueId = "l1" },
                new Team { TeamId = "t2", Name = "Team 2", LeagueId = "l1" },
                new Team { TeamId = "t3", Name = "Team 3", LeagueId = "l1" }
            };

            var seasonStart = new DateTime(2024, 4, 1);
            var seasonEnd = new DateTime(2024, 10, 1);

            // Act
            var schedule = SeasonSimulator.GenerateSchedule(league, teams, seasonStart, seasonEnd);

            // Assert
            if (schedule.Count == 0)
                throw new Exception("No games generated in schedule");

            // Each team should play each other team multiple times
            var expectedGamesPerTeam = (teams.Count - 1) * 2 * 2; // 2 opponents * 2 home+away * 2 series
            var actualGamesPerTeam = schedule
                .Where(g => g.HomeTeam.TeamId == "t1" || g.AwayTeam.TeamId == "t1")
                .Count();

            if (actualGamesPerTeam < expectedGamesPerTeam / 2) // At least half expected
                throw new Exception($"Too few games generated: {actualGamesPerTeam}");

            Console.WriteLine($"✓ Season schedule generated: {schedule.Count} games");
        }

        public void TestPlayerStatsAccumulation()
        {
            // Arrange
            var player = new Player
            {
                PlayerId = "p1",
                Name = "Test Player",
                Position = "OF",
                SeasonStats = new SeasonStats
                {
                    PlayerId = "p1",
                    GamesPlayed = 0,
                    AtBats = 0,
                    Hits = 0,
                    HomeRuns = 0,
                    RunsBattedIn = 0
                }
            };

            var initialStats = player.SeasonStats;

            // Act - Simulate stats accumulation
            player.SeasonStats.GamesPlayed += 1;
            player.SeasonStats.AtBats += 4;
            player.SeasonStats.Hits += 2;
            player.SeasonStats.HomeRuns += 1;
            player.SeasonStats.RunsBattedIn += 3;

            // Assert
            if (player.SeasonStats.GamesPlayed != 1)
                throw new Exception("Games played not incremented");

            if (player.SeasonStats.AtBats != 4)
                throw new Exception("At-bats not incremented");

            if (player.SeasonStats.Hits != 2)
                throw new Exception("Hits not incremented");

            var expectedBa = 2m / 4m;
            var actualBa = player.SeasonStats.BattingAverage;

            if (Math.Abs((double)(actualBa - (decimal)expectedBa)) > 0.001)
                throw new Exception($"Batting average incorrect: expected {expectedBa}, got {actualBa}");

            Console.WriteLine($"✓ Player stats accumulated correctly: BA = {actualBa:F3}");
        }

        public void RunAllTests()
        {
            var tests = new List<(string Name, Action Test)>
            {
                ("Probability Table Loading", TestProbabilityTableLoadsCorrectly),
                ("Event Generation Validity", TestEventGenerationProducesValidResults),
                ("Skill Affects Probability", TestBatterSkillAffectsProbability),
                ("Configuration Validation", TestConfigurationValidation),
                ("Game Simulation Completion", TestGameSimulatorCompletesGame),
                ("Season Schedule Generation", TestSeasonScheduleGeneration),
                ("Player Stats Accumulation", TestPlayerStatsAccumulation),
            };

            Console.WriteLine("\n╔═════════════════════════════════════════╗");
            Console.WriteLine("║     Baseball CLI Unit Tests              ║");
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

        private List<Player> CreateMinimalRoster(string teamPrefix)
        {
            var players = new List<Player>();

            // Pitcher
            players.Add(new Player
            {
                PlayerId = $"{teamPrefix}p1",
                Name = $"{teamPrefix} Pitcher",
                Position = "P",
                BattingAverage = 0.100m,
                SeasonStats = new SeasonStats { ERA = 4.50m }
            });

            // Position players
            var positions = new[] { "C", "1B", "2B", "3B", "SS", "LF", "CF", "RF", "DH" };
            for (int i = 0; i < positions.Length; i++)
            {
                players.Add(new Player
                {
                    PlayerId = $"{teamPrefix}b{i}",
                    Name = $"{teamPrefix} {positions[i]}",
                    Position = positions[i],
                    BattingAverage = 0.250m + (decimal)i * 0.010m,
                    SeasonStats = new SeasonStats { BattingAverage = 0.250m + (decimal)i * 0.010m }
                });
            }

            return players;
        }
    }
}
