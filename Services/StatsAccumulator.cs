using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Models;
using BaseballCli.Database;

namespace BaseballCli.Services
{
    /// <summary>
    /// Accumulates and updates player statistics during game simulation.
    /// Tracks batting stats (hits, RBIs, runs, etc.) and pitching stats (wins, ERA, strikeouts, etc.)
    /// </summary>
    public class StatsAccumulator
    {
        private readonly BaseballRepository _repository;

        public StatsAccumulator(BaseballRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Record an at-bat result for a batter.
        /// </summary>
        public void RecordAtBat(uint playerId, uint teamId, int season, PlayResultType resultType, int runsScored = 0)
        {
            var stats = _repository.GetOrCreateSeasonStats(playerId, teamId, season);

            stats.AtBats++;
            stats.Runs += runsScored;

            switch (resultType)
            {
                case PlayResultType.Single:
                    stats.Hits++;
                    break;
                case PlayResultType.Double:
                    stats.Hits++;
                    stats.Doubles++;
                    break;
                case PlayResultType.Triple:
                    stats.Hits++;
                    stats.Triples++;
                    break;
                case PlayResultType.HomeRun:
                    stats.Hits++;
                    stats.HomeRuns++;
                    stats.Runs++; // Batter always scores on home run
                    break;
                case PlayResultType.Walk:
                    stats.Walks++;
                    stats.AtBats--; // Walk doesn't count as an at-bat
                    break;
                case PlayResultType.Strikeout:
                    stats.Strikeouts++;
                    break;
                case PlayResultType.Out:
                case PlayResultType.FieldersChoice:
                case PlayResultType.Error:
                    // No additional tracking needed
                    break;
            }

            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record RBIs for a batter.
        /// </summary>
        public void RecordRBI(uint playerId, uint teamId, int season, int rbiCount)
        {
            if (rbiCount <= 0)
                return;

            var stats = _repository.GetOrCreateSeasonStats(playerId, teamId, season);
            stats.RunsBattedIn += rbiCount;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record a run scored for a player.
        /// </summary>
        public void RecordRun(uint playerId, uint teamId, int season)
        {
            var stats = _repository.GetOrCreateSeasonStats(playerId, teamId, season);
            stats.Runs++;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record stolen bases.
        /// </summary>
        public void RecordStolenBase(uint playerId, uint teamId, int season, int baseCount = 1)
        {
            var stats = _repository.GetOrCreateSeasonStats(playerId, teamId, season);
            stats.StolenBases += baseCount;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record pitching statistics for an inning or game.
        /// </summary>
        public void RecordPitchingStats(
            uint pitcherId,
            uint teamId,
            int season,
            double inningsPitched,
            int strikeouts,
            int walks,
            int earnedRuns,
            int gamesIncrement = 1)
        {
            var stats = _repository.GetOrCreateSeasonStats(pitcherId, teamId, season);
            stats.Innings += inningsPitched;
            stats.StrikeoutsPitching += strikeouts;
            stats.WalksPitching += walks;
            stats.EarnedRuns += earnedRuns;
            stats.GamesPitched += gamesIncrement;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record a win or loss for a pitcher.
        /// </summary>
        public void RecordPitcherDecision(uint pitcherId, uint teamId, int season, bool isWin)
        {
            var stats = _repository.GetOrCreateSeasonStats(pitcherId, teamId, season);
            if (isWin)
                stats.PitchingWins++;
            else
                stats.PitchingLosses++;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record a strikeout for a pitcher.
        /// </summary>
        public void RecordPitcherStrikeout(uint pitcherId, uint teamId, int season)
        {
            var stats = _repository.GetOrCreateSeasonStats(pitcherId, teamId, season);
            stats.StrikeoutsPitching++;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Record a walk allowed by a pitcher.
        /// </summary>
        public void RecordPitcherWalk(uint pitcherId, uint teamId, int season)
        {
            var stats = _repository.GetOrCreateSeasonStats(pitcherId, teamId, season);
            stats.WalksPitching++;
            stats.UpdatedAt = DateTime.UtcNow;
            _repository.AddOrUpdateSeasonStats(stats);
        }

        /// <summary>
        /// Initialize season stats for all players on a team.
        /// Call this at the start of a season.
        /// </summary>
        public void InitializeTeamSeasonStats(uint teamId, int season)
        {
            var players = _repository.GetPlayersByTeam(teamId);
            foreach (var player in players)
            {
                _repository.GetOrCreateSeasonStats(player.Id, teamId, season);
            }
        }

        /// <summary>
        /// Initialize season stats for all players in a league.
        /// Call this at the start of a season.
        /// </summary>
        public void InitializeLeagueSeasonStats(uint leagueId, int season)
        {
            var teams = _repository.GetTeamsByLeague(leagueId);
            foreach (var team in teams)
            {
                InitializeTeamSeasonStats(team.Id, season);
            }
        }

        /// <summary>
        /// Update game played count and other game-level stats for participating teams.
        /// Call this after each game is completed.
        /// </summary>
        public void UpdateGameStats(Game game, uint? winningPitcherId = null, uint? losingPitcherId = null)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            // Record wins/losses for pitchers
            if (winningPitcherId.HasValue)
            {
                var winningPitcher = _repository.GetPlayer(winningPitcherId.Value);
                RecordPitcherDecision(winningPitcherId.Value, winningPitcher.TeamId, game.Season, isWin: true);
            }

            if (losingPitcherId.HasValue)
            {
                var losingPitcher = _repository.GetPlayer(losingPitcherId.Value);
                RecordPitcherDecision(losingPitcherId.Value, losingPitcher.TeamId, game.Season, isWin: false);
            }

            // Record games played for all players
            UpdateTeamGameStats(game.HomeTeamId, game.Season);
            UpdateTeamGameStats(game.AwayTeamId, game.Season);
        }

        /// <summary>
        /// Increment games played for all players on a team for a given season.
        /// </summary>
        private void UpdateTeamGameStats(uint teamId, int season)
        {
            var players = _repository.GetPlayersByTeam(teamId);
            foreach (var player in players)
            {
                var stats = _repository.GetOrCreateSeasonStats(player.Id, teamId, season);
                stats.GamesPlayed++;
                stats.UpdatedAt = DateTime.UtcNow;
                _repository.AddOrUpdateSeasonStats(stats);
            }
        }

        /// <summary>
        /// Get a player's season statistics.
        /// </summary>
        public SeasonStats? GetPlayerSeasonStats(uint playerId, int season)
        {
            return _repository.GetSeasonStats(playerId, season);
        }

        /// <summary>
        /// Get all season statistics for a player across all seasons.
        /// </summary>
        public List<SeasonStats> GetPlayerCareerStats(uint playerId)
        {
            var player = _repository.GetPlayer(playerId);
            return player?.SeasonStats.OrderByDescending(s => s.Season).ToList() ?? new List<SeasonStats>();
        }

        /// <summary>
        /// Get league leaders for a specific stat category.
        /// </summary>
        public List<SeasonStats> GetLeagueLeaders(uint leagueId, int season, string statCategory, int limit = 10)
        {
            var teams = _repository.GetTeamsByLeague(leagueId);
            var allTeamStats = new List<SeasonStats>();
            
            // Collect stats from all teams in the league
            foreach (var team in teams)
            {
                var teamStats = _repository.GetSeasonStatsByTeam(team.Id, season);
                allTeamStats.AddRange(teamStats);
            }

            // Return leaders based on category
            return statCategory switch
            {
                "HR" => allTeamStats.OrderByDescending(s => s.HomeRuns).Take(limit).ToList(),
                "RBI" => allTeamStats.OrderByDescending(s => s.RunsBattedIn).Take(limit).ToList(),
                "AVG" => allTeamStats.Where(s => s.AtBats > 0).OrderByDescending(s => s.BattingAverage).Take(limit).ToList(),
                "R" => allTeamStats.OrderByDescending(s => s.Runs).Take(limit).ToList(),
                "H" => allTeamStats.OrderByDescending(s => s.Hits).Take(limit).ToList(),
                "SB" => allTeamStats.OrderByDescending(s => s.StolenBases).Take(limit).ToList(),
                "K" => allTeamStats.OrderByDescending(s => s.StrikeoutsPitching).Take(limit).ToList(),
                "ERA" => allTeamStats.Where(s => s.Innings > 0).OrderBy(s => s.ERA).Take(limit).ToList(),
                "W" => allTeamStats.OrderByDescending(s => s.PitchingWins).Take(limit).ToList(),
                "WHIP" => allTeamStats.Where(s => s.Innings > 0)
                    .OrderBy(s => (s.WalksPitching + s.EarnedRuns) / s.Innings)
                    .Take(limit).ToList(),
                _ => new List<SeasonStats>()
            };
        }
    }

    /// <summary>
    /// Enumeration of play result types for stat tracking.
    /// </summary>
    public enum PlayResultType
    {
        Single,
        Double,
        Triple,
        HomeRun,
        Walk,
        Strikeout,
        Out,
        FieldersChoice,
        Error,
        StolenBase
    }
}
