using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Config;
using BaseballCli.Database;
using BaseballCli.Models;

namespace BaseballCli.Services
{
    /// <summary>
    /// Simulates an entire season: game scheduling, execution, and stats aggregation.
    /// </summary>
    public class SeasonSimulator
    {
        private readonly GameSimulator _gameSimulator;
        private readonly BaseballRepository _repository;
        private readonly RulesConfig _rules;
        private readonly Random _random;

        public SeasonSimulator(
            GameSimulator gameSimulator,
            BaseballRepository repository,
            RulesConfig rules,
            int? seed = null)
        {
            _gameSimulator = gameSimulator;
            _repository = repository;
            _rules = rules;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Generates the complete schedule for a season.
        /// </summary>
        public List<Game> GenerateSchedule(League league, List<Team> teams, int season)
        {
            var games = new List<Game>();
            DateTime startDate = DateTime.Parse(_rules.StartDate);
            int gameNum = 0;

            // Simple round-robin scheduling
            for (int round = 0; round < _rules.SeasonLength / (teams.Count * (_rules.GamesPerSeries - 1) / 2); round++)
            {
                for (int i = 0; i < teams.Count; i++)
                {
                    for (int j = i + 1; j < teams.Count; j++)
                    {
                        // Home and away series
                        for (int seriesGame = 0; seriesGame < _rules.GamesPerSeries; seriesGame++)
                        {
                            if (gameNum >= _rules.SeasonLength)
                                break;

                            var game = new Game
                            {
                                LeagueId = league.Id,
                                HomeTeamId = teams[i].Id,
                                AwayTeamId = teams[j].Id,
                                GameDate = startDate.AddDays(gameNum),
                                Season = season,
                                Status = "NotStarted",
                                CreatedAt = DateTime.Now
                            };

                            games.Add(game);
                            gameNum++;

                            if (gameNum >= _rules.SeasonLength)
                                goto ScheduleComplete;

                            // Away series
                            var awayGame = new Game
                            {
                                LeagueId = league.Id,
                                HomeTeamId = teams[j].Id,
                                AwayTeamId = teams[i].Id,
                                GameDate = startDate.AddDays(gameNum),
                                Season = season,
                                Status = "NotStarted",
                                CreatedAt = DateTime.Now
                            };

                            games.Add(awayGame);
                            gameNum++;

                            if (gameNum >= _rules.SeasonLength)
                                goto ScheduleComplete;
                        }
                    }
                }
            }

            ScheduleComplete:
            return games.Take(_rules.SeasonLength).ToList();
        }

        /// <summary>
        /// Simulates a range of games and updates statistics.
        /// </summary>
        public SeasonSimulationResult SimulateGames(
            League league,
            List<Team> teams,
            List<Game> games,
            DateTime fromDate,
            DateTime toDate,
            int season,
            string weather = "Clear")
        {
            var result = new SeasonSimulationResult
            {
                SeasonId = league.Id.ToString(),
                GamesSimulated = new List<GameResult>()
            };

            var gamesToSimulate = games
                .Where(g => g.GameDate.Date >= fromDate.Date && g.GameDate.Date <= toDate.Date && g.Status == "NotStarted")
                .ToList();

            // Initialize season stats if not present
            foreach (var team in teams)
            {
                var teamStats = _repository.GetTeamStats(team.Id, season);
                if (teamStats == null)
                {
                    teamStats = new TeamStats
                    {
                        TeamId = team.Id,
                        Season = season,
                        Wins = 0,
                        Losses = 0,
                        RunsFor = 0,
                        RunsAgainst = 0
                    };
                    _repository.AddOrUpdateTeamStats(teamStats);
                }

                foreach (var player in team.Players)
                {
                    var playerStats = _repository.GetSeasonStats(player.Id, season);
                    if (playerStats == null)
                    {
                        playerStats = new SeasonStats
                        {
                            PlayerId = player.Id,
                            TeamId = team.Id,
                            Season = season,
                            GamesPlayed = 0
                        };
                        _repository.AddOrUpdateSeasonStats(playerStats);
                    }
                }
            }

            // Simulate each game
            foreach (var game in gamesToSimulate)
            {
                var homeTeam = teams.FirstOrDefault(t => t.Id == game.HomeTeamId);
                var awayTeam = teams.FirstOrDefault(t => t.Id == game.AwayTeamId);

                if (homeTeam == null || awayTeam == null)
                    continue;

                int dayOfSeason = (game.GameDate.Date - DateTime.Parse(_rules.StartDate).Date).Days + 1;

                var gameResult = _gameSimulator.SimulateGame(
                    game,
                    homeTeam.Players.ToList(),
                    awayTeam.Players.ToList(),
                    dayOfSeason,
                    weather
                );

                result.GamesSimulated.Add(gameResult);

                // Update team stats
                UpdateTeamStats(homeTeam.Id, awayTeam.Id, game.HomeScore, game.AwayScore, season);

                // Update player stats
                UpdatePlayerStats(homeTeam, awayTeam, gameResult.Plays, season);

                // Save game to database
                _repository.AddGame(game);
                foreach (var play in gameResult.Plays)
                {
                    var playEntity = new Play
                    {
                        GameId = game.Id,
                        Inning = play.Inning,
                        PlayNumber = play.PlayNumber,
                        BatterId = play.AtBatResult?.Batter.Id ?? 0,
                        PitcherId = play.AtBatResult?.Pitcher.Id ?? 0,
                        BatterTeamId = play.AtBatResult?.Context.IsHomeTeam ?? false ? game.HomeTeamId : game.AwayTeamId,
                        EventType = play.AtBatResult?.EventType ?? "Unknown",
                        Result = play.AtBatResult?.Result ?? "Unknown",
                        Outs = play.AtBatResult?.Context.Outs ?? 0,
                        RunnersOnBase = play.AtBatResult?.Context.Outs.ToString()
                    };
                    _repository.AddPlay(playEntity);
                }
            }

            result.TotalGamesSimulated = gamesToSimulate.Count;
            return result;
        }

        private void UpdateTeamStats(uint homeTeamId, uint awayTeamId, int homeScore, int awayScore, int season)
        {
            var homeStats = _repository.GetTeamStats(homeTeamId, season);
            var awayStats = _repository.GetTeamStats(awayTeamId, season);

            if (homeStats != null)
            {
                homeStats.RunsFor += homeScore;
                homeStats.RunsAgainst += awayScore;
                if (homeScore > awayScore)
                    homeStats.Wins++;
                else
                    homeStats.Losses++;
                _repository.AddOrUpdateTeamStats(homeStats);
            }

            if (awayStats != null)
            {
                awayStats.RunsFor += awayScore;
                awayStats.RunsAgainst += homeScore;
                if (awayScore > homeScore)
                    awayStats.Wins++;
                else
                    awayStats.Losses++;
                _repository.AddOrUpdateTeamStats(awayStats);
            }
        }

        private void UpdatePlayerStats(Team homeTeam, Team awayTeam, List<PlayEvent> plays, int season)
        {
            var playerPlayCounts = new Dictionary<uint, int>();

            foreach (var play in plays)
            {
                if (play.AtBatResult != null)
                {
                    var batterId = play.AtBatResult.Batter.Id;
                    if (!playerPlayCounts.ContainsKey(batterId))
                        playerPlayCounts[batterId] = 0;
                    playerPlayCounts[batterId]++;
                }
            }

            foreach (var kvp in playerPlayCounts)
            {
                var stats = _repository.GetSeasonStats(kvp.Key, season);
                if (stats != null)
                {
                    stats.GamesPlayed++;
                    stats.AtBats += kvp.Value;
                    _repository.AddOrUpdateSeasonStats(stats);
                }
            }
        }

        /// <summary>
        /// Gets current standings for the season.
        /// </summary>
        public List<TeamStats> GetStandings(uint leagueId, int season)
        {
            return _repository.GetLeagueStandings(leagueId, season);
        }

        /// <summary>
        /// Gets leader stats for the season (batting average, home runs, etc.).
        /// </summary>
        public SeasonLeaders GetSeasonLeaders(uint leagueId, int season, List<Team> teams)
        {
            var leaders = new SeasonLeaders();
            var allStats = new List<SeasonStats>();

            foreach (var team in teams)
            {
                allStats.AddRange(_repository.GetSeasonStatsByTeam(team.Id, season));
            }

            if (allStats.Count > 0)
            {
                leaders.BattingAverage = allStats.OrderByDescending(s => s.BattingAverage).FirstOrDefault();
                leaders.HomeRuns = allStats.OrderByDescending(s => s.HomeRuns).FirstOrDefault();
                leaders.StrikeOuts = allStats.OrderByDescending(s => s.Strikeouts).FirstOrDefault();
                leaders.ERA = allStats.OrderBy(s => s.ERA).FirstOrDefault();
                leaders.Wins = allStats.OrderByDescending(s => s.PitchingWins).FirstOrDefault();
            }

            return leaders;
        }
    }

    /// <summary>
    /// Result of a season simulation period.
    /// </summary>
    public class SeasonSimulationResult
    {
        public string SeasonId { get; set; } = "";
        public int TotalGamesSimulated { get; set; }
        public List<GameResult> GamesSimulated { get; set; } = new();

        public string GetSummary()
        {
            return $"Simulated {TotalGamesSimulated} games. Total plays: {GamesSimulated.Sum(g => g.Plays.Count)}";
        }
    }

    /// <summary>
    /// Season leaders in various statistical categories.
    /// </summary>
    public class SeasonLeaders
    {
        public SeasonStats? BattingAverage { get; set; }
        public SeasonStats? HomeRuns { get; set; }
        public SeasonStats? StrikeOuts { get; set; }
        public SeasonStats? ERA { get; set; }
        public SeasonStats? Wins { get; set; }
    }
}
