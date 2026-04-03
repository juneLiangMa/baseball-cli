using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Config;
using BaseballCli.Database;
using BaseballCli.Models;

namespace BaseballCli.Services
{
    /// <summary>
    /// Simulates a single baseball game inning-by-inning, play-by-play.
    /// </summary>
    public class GameSimulator
    {
        private readonly EventGenerator _eventGenerator;
        private readonly SimulationAlgorithms _algorithms;
        private readonly BaseballRepository _repository;
        private readonly Random _random;

        public GameSimulator(
            EventGenerator eventGenerator,
            SimulationAlgorithms algorithms,
            BaseballRepository repository,
            int? seed = null)
        {
            _eventGenerator = eventGenerator;
            _algorithms = algorithms;
            _repository = repository;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Simulates a complete game and returns the result.
        /// </summary>
        public GameResult SimulateGame(Game game, List<Player> homeTeamPlayers, List<Player> awayTeamPlayers, int dayOfSeason, string weather = "Clear")
        {
            var result = new GameResult
            {
                Game = game,
                Plays = new List<PlayEvent>(),
                HomeTeamPlayers = homeTeamPlayers,
                AwayTeamPlayers = awayTeamPlayers
            };

            // Initialize game state
            int homeScore = 0;
            int awayScore = 0;
            int inning = 1;
            int playNumber = 0;

            // Simulate each inning
            while (inning <= 9)
            {
                // Top of inning (away team bats)
                var topResult = SimulateHalfInning(
                    inning,
                    true,
                    awayTeamPlayers,
                    homeTeamPlayers,
                    ref playNumber,
                    dayOfSeason,
                    weather,
                    game.AwayTeamId,
                    game.HomeTeamId
                );
                awayScore += topResult.RunsScored;
                result.Plays.AddRange(topResult.Plays);

                // Bottom of inning (home team bats)
                var bottomResult = SimulateHalfInning(
                    inning,
                    false,
                    homeTeamPlayers,
                    awayTeamPlayers,
                    ref playNumber,
                    dayOfSeason,
                    weather,
                    game.HomeTeamId,
                    game.AwayTeamId
                );
                homeScore += bottomResult.RunsScored;
                result.Plays.AddRange(bottomResult.Plays);

                inning++;
            }

            // Determine winner
            game.HomeScore = homeScore;
            game.AwayScore = awayScore;
            game.Status = "Completed";
            game.UpdatedAt = DateTime.Now;

            result.IsComplete = true;
            result.Winner = homeScore > awayScore ? game.HomeTeam : game.AwayTeam;

            return result;
        }

        private HalfInningResult SimulateHalfInning(
            int inning,
            bool isTopOfInning,
            List<Player> battingTeam,
            List<Player> pitchingTeam,
            ref int playNumber,
            int dayOfSeason,
            string weather,
            string battingTeamId,
            string pitchingTeamId)
        {
            var result = new HalfInningResult { Plays = new List<PlayEvent>(), RunsScored = 0 };

            int outs = 0;
            int runsThisInning = 0;
            var bases = new BaseState();

            // Select starting pitcher
            var pitcher = pitchingTeam.FirstOrDefault(p => p.Position == "Pitcher") ?? pitchingTeam[0];
            var battingOrder = CreateBattingOrder(battingTeam);
            int battingOrderIndex = 0;

            // Continue until 3 outs
            while (outs < 3)
            {
                playNumber++;
                var batter = battingOrder[battingOrderIndex % battingOrder.Count];

                // Create context for this at-bat
                var context = new SimulationContext
                {
                    Inning = inning,
                    Outs = outs,
                    DayOfSeason = dayOfSeason,
                    IsHomeTeam = !isTopOfInning,
                    Weather = weather,
                    BatterGamesPlayedRecently = 0, // TODO: track from season
                    PitcherGamesPlayedRecently = 0  // TODO: track from season
                };

                // Generate at-bat result
                var atBatResult = _eventGenerator.GenerateAtBatResult(batter, pitcher, context);

                // Create play event
                var playEvent = new PlayEvent
                {
                    Inning = inning,
                    PlayNumber = playNumber,
                    Description = $"{batter.Name} {atBatResult.Result}",
                    AtBatResult = atBatResult
                };

                // Update bases and outs
                if (atBatResult.BaseAwarded > 0)
                {
                    // Advance bases
                    AdvanceBases(bases, atBatResult.BaseAwarded, out int runsScored);
                    runsThisInning += runsScored;
                    playEvent.Description += $" ({runsScored} run" + (runsScored != 1 ? "s" : "") + ")";
                }
                else if (atBatResult.Result.EndsWith("Out"))
                {
                    outs++;
                    playEvent.Description += $" (Out #{outs})";
                }

                result.Plays.Add(playEvent);
                battingOrderIndex++;
            }

            result.RunsScored = runsThisInning;
            return result;
        }

        private List<Player> CreateBattingOrder(List<Player> team)
        {
            // Simple batting order: pitchers last, others in defensive position order
            var hitters = team.Where(p => p.Position != "Pitcher").ToList();
            var pitchers = team.Where(p => p.Position == "Pitcher").ToList();

            var order = new List<Player>();
            if (hitters.Count > 0)
            {
                order.AddRange(hitters);
                order.AddRange(pitchers);
            }
            else
            {
                order.AddRange(team);
            }

            // Use first 9 for the batting order
            return order.Take(9).ToList();
        }

        private void AdvanceBases(BaseState bases, int basesAwarded, out int runsScored)
        {
            runsScored = 0;

            // Scoring runners from 3rd
            if ((bases.Third ?? false) && basesAwarded > 0)
                runsScored++;

            // Advance runners
            if (basesAwarded >= 3)
            {
                // Runner from 1st scores on triple
                bases.Third = bases.First != null;
                bases.Second = null;
                bases.First = null;
            }
            else if (basesAwarded == 2)
            {
                bases.Third = bases.Second != null;
                bases.Second = bases.First != null;
                bases.First = null;
            }
            else if (basesAwarded == 1)
            {
                bases.Third = bases.Second != null;
                bases.Second = bases.First != null;
                bases.First = true; // New batter on first
            }

            // Check if batter-runner scores (home run)
            if (basesAwarded >= 4)
                runsScored++;
        }

        /// <summary>
        /// Simulates the outcome of a specific play (e.g., caught stealing).
        /// </summary>
        public void SimulateSpecialPlay(string playType, BaseState bases)
        {
            switch (playType)
            {
                case "CaughtStealing":
                    // Remove runner attempting to steal
                    if (bases.First ?? false)
                        bases.First = false;
                    break;

                case "DoublePlaying":
                    // Remove two runners
                    bases.First = false;
                    if (bases.Second ?? false)
                        bases.Second = false;
                    break;

                case "WildPitch":
                    // Advance bases by one
                    if (bases.Third ?? false)
                        bases.Third = bases.Second ?? false;
                    if (bases.Second ?? false)
                        bases.Second = bases.First ?? false;
                    break;
            }
        }
    }

    /// <summary>
    /// Represents the state of all bases (1st, 2nd, 3rd).
    /// Simplified for MVP: just track if occupied.
    /// </summary>
    public class BaseState
    {
        public bool? First { get; set; }
        public bool? Second { get; set; }
        public bool? Third { get; set; }

        public string GetEncoding()
        {
            // Return encoding like "1--" for runner on first
            return (First.HasValue && First.Value ? "1" : "-") +
                   (Second.HasValue && Second.Value ? "2" : "-") +
                   (Third.HasValue && Third.Value ? "3" : "-");
        }
    }

    /// <summary>
    /// Result of a complete game simulation.
    /// </summary>
    public class GameResult
    {
        public Game Game { get; set; } = null!;
        public List<PlayEvent> Plays { get; set; } = new();
        public List<Player> HomeTeamPlayers { get; set; } = new();
        public List<Player> AwayTeamPlayers { get; set; } = new();
        public bool IsComplete { get; set; }
        public Team? Winner { get; set; }

        public string GetSummary()
        {
            return $"{Game.AwayTeam.Name} {Game.AwayScore} @ {Game.HomeTeam.Name} {Game.HomeScore}";
        }

        public string GetDetailedSummary()
        {
            var lines = new List<string>
            {
                $"Game on {Game.GameDate:MMMM d, yyyy}",
                $"{Game.AwayTeam.City} {Game.AwayTeam.Name} ({Game.AwayScore}) @ {Game.HomeTeam.City} {Game.HomeTeam.Name} ({Game.HomeScore})",
                $"Winner: {Winner?.Name ?? "Tie"}",
                $"Total Plays: {Plays.Count}"
            };

            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// Result of a half-inning (top or bottom).
    /// </summary>
    public class HalfInningResult
    {
        public List<PlayEvent> Plays { get; set; } = new();
        public int RunsScored { get; set; }
        public int Outs { get; set; }
    }
}
