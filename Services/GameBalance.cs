using System;
using System.Collections.Generic;
using BaseballCli.Config;
using BaseballCli.Models;

namespace BaseballCli.Services
{
    /// <summary>
    /// Game balance and tuning utilities.
    /// Provides methods to analyze and adjust probability weights for realistic gameplay.
    /// </summary>
    public class GameBalance
    {
        /// <summary>
        /// Analyzes season statistics to detect imbalances.
        /// Flags teams with extreme win percentages or unrealistic stats.
        /// </summary>
        public static (bool IsBalanced, List<string> Issues) AnalyzeSeasonBalance(List<Team> teams, int gamesPlayed)
        {
            var issues = new List<string>();

            if (teams.Count < 2)
            {
                issues.Add("Not enough teams for balance analysis");
                return (false, issues);
            }

            // Analyze win distributions
            var winPercentages = new List<decimal>();
            decimal totalWins = 0;
            decimal maxWins = 0;
            decimal minWins = decimal.MaxValue;

            foreach (var team in teams)
            {
                var winPct = team.WinPercentage;
                winPercentages.Add(winPct);
                totalWins += team.Wins;

                if (team.Wins > maxWins)
                    maxWins = team.Wins;
                if (team.Wins < minWins)
                    minWins = team.Wins;
            }

            var avgWinPct = totalWins / (teams.Count * gamesPlayed);

            // Check for excessive variance
            if (maxWins - minWins > gamesPlayed / 2)
            {
                issues.Add($"High win variance: {maxWins - minWins} win difference between best and worst teams");
            }

            // Check for unbalanced league
            var variance = CalculateVariance(winPercentages);
            if (variance > 0.05m) // Win % variance > 5%
            {
                issues.Add($"League appears unbalanced: Win % variance is {variance:F3}");
            }

            return (issues.Count == 0, issues);
        }

        /// <summary>
        /// Suggests probability adjustments based on observed player performance.
        /// </summary>
        public static List<(string Description, decimal SuggestedMultiplier)> SuggestProbabilityAdjustments(
            Player batter, Player pitcher, decimal observedHitRate)
        {
            var suggestions = new List<(string, decimal)>();

            // If batter is hitting too well (>0.40), suggest lower multiplier
            if (observedHitRate > 0.40m && (batter.SeasonStats?.BattingAverage ?? 0) > 0.320m)
            {
                suggestions.Add(
                    ($"Batter {batter.Name} hitting too well ({observedHitRate:F3}), consider lowering multiplier", 0.9m)
                );
            }

            // If batter is hitting too poorly (<0.15), suggest higher multiplier
            if (observedHitRate < 0.15m && (batter.SeasonStats?.BattingAverage ?? 0) > 0.250m)
            {
                suggestions.Add(
                    ($"Batter {batter.Name} hitting too poorly ({observedHitRate:F3}), consider raising multiplier", 1.1m)
                );
            }

            return suggestions;
        }

        /// <summary>
        /// Validates that probability tables produce realistic scoring.
        /// </summary>
        public static (bool IsValid, List<string> Issues) ValidateScoringRealism(List<Game> games)
        {
            var issues = new List<string>();

            if (games.Count < 5)
                return (true, issues); // Not enough data

            decimal avgHomeScore = 0;
            decimal avgAwayScore = 0;
            int extremeScores = 0;

            foreach (var game in games)
            {
                avgHomeScore += game.HomeTeamScore;
                avgAwayScore += game.AwayTeamScore;

                // Extreme score check (realistic baseball games rarely exceed 15 runs)
                if (game.HomeTeamScore > 20 || game.AwayTeamScore > 20)
                {
                    extremeScores++;
                }
            }

            avgHomeScore /= games.Count;
            avgAwayScore /= games.Count;

            // Average score in MLB is around 4-5 runs per team per game
            if (avgHomeScore < 2 || avgHomeScore > 12)
            {
                issues.Add($"Home team average score unrealistic: {avgHomeScore:F2} (expected 4-5)");
            }

            if (avgAwayScore < 2 || avgAwayScore > 12)
            {
                issues.Add($"Away team average score unrealistic: {avgAwayScore:F2} (expected 4-5)");
            }

            if (extremeScores > games.Count * 0.1m) // More than 10% extreme scores
            {
                issues.Add($"Too many extreme scores: {extremeScores}/{games.Count}");
            }

            return (issues.Count == 0, issues);
        }

        /// <summary>
        /// Validates home field advantage effect.
        /// Home teams should win roughly 54% of games.
        /// </summary>
        public static (bool IsValid, decimal HomeWinPercentage) ValidateHomeFieldAdvantage(List<Game> games)
        {
            if (games.Count == 0)
                return (true, 0);

            int homeWins = 0;
            foreach (var game in games)
            {
                if (game.HomeTeamScore > game.AwayTeamScore)
                    homeWins++;
            }

            var homeWinPct = (decimal)homeWins / games.Count;

            // Valid range: 0.50-0.58 (home field advantage should exist but not be extreme)
            var isValid = homeWinPct >= 0.50m && homeWinPct <= 0.58m;

            return (isValid, homeWinPct);
        }

        /// <summary>
        /// Detects and reports on potential issues in event distribution.
        /// </summary>
        public static (bool IsValid, List<string> Issues) ValidateEventDistribution(
            Dictionary<string, int> eventCounts, int totalAtBats)
        {
            var issues = new List<string>();

            if (totalAtBats < 50)
                return (true, issues); // Not enough data

            // Expected ranges based on MLB averages
            var expectedRanges = new Dictionary<string, (decimal Min, decimal Max)>
            {
                { "Hit", (0.25m, 0.35m) },
                { "Walk", (0.07m, 0.12m) },
                { "Strikeout", (0.15m, 0.25m) },
                { "HomeRun", (0.02m, 0.05m) },
                { "Out", (0.30m, 0.40m) }
            };

            foreach (var (eventType, (minExpected, maxExpected)) in expectedRanges)
            {
                if (eventCounts.TryGetValue(eventType, out var count))
                {
                    var percentage = (decimal)count / totalAtBats;

                    if (percentage < minExpected)
                    {
                        issues.Add($"{eventType} rate too low: {percentage:F3} (expected {minExpected:F3}-{maxExpected:F3})");
                    }
                    else if (percentage > maxExpected)
                    {
                        issues.Add($"{eventType} rate too high: {percentage:F3} (expected {minExpected:F3}-{maxExpected:F3})");
                    }
                }
            }

            return (issues.Count == 0, issues);
        }

        private static decimal CalculateVariance(List<decimal> values)
        {
            if (values.Count == 0)
                return 0;

            var mean = 0m;
            foreach (var v in values)
                mean += v;
            mean /= values.Count;

            var variance = 0m;
            foreach (var v in values)
            {
                variance += (v - mean) * (v - mean);
            }

            return variance / values.Count;
        }
    }

    /// <summary>
    /// Edge case handling for simulation.
    /// </summary>
    public static class EdgeCaseHandling
    {
        /// <summary>
        /// Validates game score is within reasonable bounds after simulation.
        /// </summary>
        public static bool IsValidGameScore(int homeScore, int awayScore)
        {
            // Reasonable bounds: 0-20 for each team
            return homeScore >= 0 && homeScore <= 20 && awayScore >= 0 && awayScore <= 20;
        }

        /// <summary>
        /// Handles edge case where a player has no stats recorded.
        /// </summary>
        public static decimal GetSafePlayerAverage(Player player)
        {
            if (player?.SeasonStats?.AtBats == 0)
                return 0.250m; // League average for unknown players
            return player?.SeasonStats?.BattingAverage ?? 0;
        }

        /// <summary>
        /// Validates that a game has valid teams and players.
        /// </summary>
        public static bool IsValidGame(Game game)
        {
            if (game == null)
                return false;

            if (game.HomeTeam == null || game.AwayTeam == null)
                return false;

            if (game.HomeTeam.TeamId == game.AwayTeam.TeamId)
                return false; // Teams can't play themselves

            if (game.HomeTeam.Players.Count < 9 || game.AwayTeam.Players.Count < 9)
                return false; // Not enough players

            return true;
        }

        /// <summary>
        /// Normalizes a probability multiplier to valid range [0.5, 2.0].
        /// </summary>
        public static decimal NormalizeMultiplier(decimal multiplier)
        {
            if (multiplier < 0.5m)
                return 0.5m;
            if (multiplier > 2.0m)
                return 2.0m;
            return multiplier;
        }

        /// <summary>
        /// Handles division by zero in statistics calculations.
        /// </summary>
        public static decimal SafeDivide(int numerator, int denominator, decimal defaultValue = 0)
        {
            if (denominator == 0)
                return defaultValue;
            return (decimal)numerator / denominator;
        }
    }
}
