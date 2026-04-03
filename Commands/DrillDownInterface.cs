using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Database;
using BaseballCli.Models;
using Spectre.Console;

namespace BaseballCli.Commands
{
    /// <summary>
    /// Allows drilling down into specific games and plays for detailed analysis.
    /// </summary>
    public class DrillDownInterface
    {
        private readonly BaseballDbContext _db;
        private readonly BaseballRepository _repository;
        private readonly RealTimeViewer _viewer;

        public DrillDownInterface(BaseballDbContext db)
        {
            _db = db;
            _repository = new BaseballRepository(db);
            _viewer = new RealTimeViewer(RealTimeViewer.Verbosity.Verbose);
        }

        public void DisplayLeagueSchedule(League league, DateTime? specificDate = null)
        {
            var games = _repository.GetGamesByLeague(league.Id);

            if (specificDate.HasValue)
            {
                games = games
                    .Where(g => g.GameDate.Date == specificDate.Value.Date)
                    .ToList();

                if (!games.Any())
                {
                    AnsiConsole.MarkupLine($"[yellow]No games scheduled for {specificDate:yyyy-MM-dd}[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[bold cyan]Games for {specificDate:yyyy-MM-dd}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold cyan]{league.Name} Schedule[/]");
            }

            var table = new Table();
            table.AddColumn("Date");
            table.AddColumn("Away");
            table.AddColumn("Home");
            table.AddColumn("Result");
            table.AddColumn("Status");

            foreach (var game in games.OrderBy(g => g.GameDate))
            {
                var status = game.IsCompleted ? "Final" : "Scheduled";
                var result = game.IsCompleted
                    ? $"{game.AwayScore}-{game.HomeScore}"
                    : "—";

                var resultColor = game.IsCompleted
                    ? (game.HomeScore > game.AwayScore ? "[green]" : "[red]")
                    : "[yellow]";

                table.AddRow(
                    game.GameDate.ToString("yyyy-MM-dd"),
                    game.AwayTeam?.Name ?? "?",
                    game.HomeTeam?.Name ?? "?",
                    $"{resultColor}{result}[/]",
                    status
                );
            }

            AnsiConsole.Write(table);
        }

        public void DisplayGameDetails(Game game)
        {
            if (!game.IsCompleted)
            {
                AnsiConsole.MarkupLine("[yellow]Game not completed yet[/]");
                return;
            }

            var winner = game.HomeScore > game.AwayScore ? game.HomeTeam : game.AwayTeam;
            var loser = game.HomeScore > game.AwayScore ? game.AwayTeam : game.HomeTeam;

            AnsiConsole.MarkupLine($"[bold cyan]{game.AwayTeam?.Name} @ {game.HomeTeam?.Name}[/]");
            AnsiConsole.MarkupLine($"[yellow]Date:[/] {game.GameDate:yyyy-MM-dd}");
            AnsiConsole.MarkupLine($"[yellow]Result:[/] {winner?.Name} won {Math.Max(game.HomeScore, game.AwayScore)}-{Math.Min(game.HomeScore, game.AwayScore)}");
            AnsiConsole.WriteLine();

            // Get plays for this game
            var plays = _repository.GetPlaysByGame(game.GameId);

            if (!plays.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No play details recorded[/]");
                return;
            }

            // Group plays by inning
            var playsByInning = plays.GroupBy(p => p.Inning).OrderBy(g => g.Key);

            foreach (var inningGroup in playsByInning)
            {
                AnsiConsole.MarkupLine($"[bold yellow]Inning {inningGroup.Key}[/]");

                var table = new Table();
                table.AddColumn("Time");
                table.AddColumn("Batter");
                table.AddColumn("Pitcher");
                table.AddColumn("Event");
                table.AddColumn("Details");

                foreach (var play in inningGroup.OrderBy(p => p.PlaySequence))
                {
                    var time = play.PlaySequence;
                    var eventColor = GetEventColor(play.EventType);

                    table.AddRow(
                        time.ToString(),
                        play.Batter?.Name ?? "?",
                        play.Pitcher?.Name ?? "?",
                        $"[{eventColor}]{play.EventType}[/]",
                        play.EventDetails ?? "—"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
        }

        public void DisplayInningByInning(Game game)
        {
            if (!game.IsCompleted)
            {
                AnsiConsole.MarkupLine("[yellow]Game not completed yet[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[bold cyan]{game.AwayTeam?.Name} @ {game.HomeTeam?.Name} - Inning-by-Inning[/]");

            var table = new Table();
            table.AddColumn(new TableColumn("Inning").Centered());
            table.AddColumn(new TableColumn(game.AwayTeam?.Name ?? "Away").Centered());
            table.AddColumn(new TableColumn(game.HomeTeam?.Name ?? "Home").Centered());

            var plays = _repository.GetPlaysByGame(game.GameId).ToList();
            var playsByInning = plays.GroupBy(p => p.Inning).OrderBy(g => g.Key);

            int awayScore = 0;
            int homeScore = 0;

            foreach (var inningGroup in playsByInning)
            {
                // Calculate runs scored in this inning
                var inningAwayRuns = 0;
                var inningHomeRuns = 0;

                // This is simplified - in a real implementation, you'd track this from play details
                // For now, we just show what happened

                table.AddRow(
                    inningGroup.Key.ToString(),
                    "—",
                    "—"
                );
            }

            AnsiConsole.Write(table);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Final: {game.AwayTeam?.Name} {game.AwayScore}, {game.HomeTeam?.Name} {game.HomeScore}[/]");
        }

        public void DisplayTeamGameLog(Team team, int limit = 10)
        {
            var games = _repository.GetGamesByTeam(team.Id)
                .Where(g => g.IsCompleted)
                .OrderByDescending(g => g.GameDate)
                .Take(limit)
                .ToList();

            if (!games.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No completed games for {team.Name}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[bold cyan]{team.Name} - Recent Games[/]");

            var table = new Table();
            table.AddColumn("Date");
            table.AddColumn("Opponent");
            table.AddColumn("Result");
            table.AddColumn("Score");

            foreach (var game in games)
            {
                var opponent = game.HomeTeam?.Id == team.Id ? game.AwayTeam : game.HomeTeam;
                var teamScore = game.HomeTeam?.Id == team.Id ? game.HomeScore : game.AwayScore;
                var oppScore = game.HomeTeam?.Id == team.Id ? game.AwayScore : game.HomeScore;

                var result = teamScore > oppScore ? "[green]W[/]" : "[red]L[/]";
                var resultStr = teamScore > oppScore ? "W" : "L";

                table.AddRow(
                    game.GameDate.ToString("yyyy-MM-dd"),
                    opponent?.Name ?? "?",
                    result,
                    $"{teamScore}-{oppScore}"
                );
            }

            AnsiConsole.Write(table);
        }

        public void DisplayPlayerGameLog(Player player, int limit = 10)
        {
            var plays = _repository.GetPlaysByPlayer(player.Id)
                .GroupBy(p => p.GameId)
                .OrderByDescending(g => g.First().Game?.GameDate)
                .Take(limit)
                .ToList();

            if (!plays.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No games played by {player.Name}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[bold cyan]{player.Name} - Recent Games[/]");

            var table = new Table();
            table.AddColumn("Date");
            table.AddColumn("Opponent");
            table.AddColumn("AB");
            table.AddColumn("H");
            table.AddColumn("HR");
            table.AddColumn("RBI");

            foreach (var gameGroup in plays)
            {
                var game = gameGroup.First().Game;
                var opponent = game?.HomeTeam?.Id == player.Id ? game?.AwayTeam : game?.HomeTeam;

                // Count stats from plays
                var atBats = gameGroup.Count(p => new[] { "Hit", "Out", "Strikeout", "Groundout" }.Contains(p.EventType));
                var hits = gameGroup.Count(p => new[] { "Hit", "Double", "Triple", "HomeRun" }.Contains(p.EventType));
                var homeRuns = gameGroup.Count(p => p.EventType == "HomeRun");

                table.AddRow(
                    game?.GameDate.ToString("yyyy-MM-dd") ?? "?",
                    opponent?.Name ?? "?",
                    atBats.ToString(),
                    hits.ToString(),
                    homeRuns.ToString(),
                    "—"
                );
            }

            AnsiConsole.Write(table);
        }

        private static string GetEventColor(string eventType)
        {
            return eventType switch
            {
                "Hit" => "green",
                "HomeRun" => "bold green",
                "Double" => "green",
                "Triple" => "bold green",
                "Walk" => "cyan",
                "Strikeout" => "red",
                "Out" => "red",
                "DoublePlay" => "bold red",
                "Error" => "yellow",
                "StolenBase" => "blue",
                _ => "white"
            };
        }

        public void DisplayPlayerComparison(Player player1, Player player2)
        {
            var table = new Table();
            table.Title = new TableTitle("[bold]Player Comparison[/]");
            table.AddColumn("Stat");
            table.AddColumn(player1.Name);
            table.AddColumn(player2.Name);

            var stats1 = player1.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();
            var stats2 = player2.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();

            if (player1.Position != "P" && player2.Position != "P")
            {
                // Batting comparison
                table.AddRow("Position", player1.Position, player2.Position);
                table.AddRow("G", stats1?.GamesPlayed.ToString() ?? "0", stats2?.GamesPlayed.ToString() ?? "0");
                table.AddRow("AB", stats1?.AtBats.ToString() ?? "0", stats2?.AtBats.ToString() ?? "0");
                table.AddRow("H", stats1?.Hits.ToString() ?? "0", stats2?.Hits.ToString() ?? "0");
                var avg1 = stats1?.AtBats > 0 ? (double)stats1.Hits / stats1.AtBats : 0;
                var avg2 = stats2?.AtBats > 0 ? (double)stats2.Hits / stats2.AtBats : 0;
                table.AddRow("AVG", avg1.ToString("F3"), avg2.ToString("F3"));
                table.AddRow("HR", stats1?.HomeRuns.ToString() ?? "0", stats2?.HomeRuns.ToString() ?? "0");
                table.AddRow("RBI", stats1?.RunsBattedIn.ToString() ?? "0", stats2?.RunsBattedIn.ToString() ?? "0");
            }
            else if (player1.Position == "P" && player2.Position == "P")
            {
                // Pitching comparison
                table.AddRow("G", stats1?.GamesPitched.ToString() ?? "0", stats2?.GamesPitched.ToString() ?? "0");
                table.AddRow("W-L", $"{stats1?.PitchingWins ?? 0}-{stats1?.PitchingLosses ?? 0}", 
                    $"{stats2?.PitchingWins ?? 0}-{stats2?.PitchingLosses ?? 0}");
                var era1 = stats1?.Innings > 0 ? (double)(stats1.EarnedRuns * 9) / stats1.Innings : 0;
                var era2 = stats2?.Innings > 0 ? (double)(stats2.EarnedRuns * 9) / stats2.Innings : 0;
                table.AddRow("ERA", era1.ToString("F2"), era2.ToString("F2"));
                table.AddRow("IP", stats1?.Innings.ToString("F1") ?? "0", stats2?.Innings.ToString("F1") ?? "0");
                table.AddRow("SO", stats1?.StrikeoutsPitching.ToString() ?? "0", stats2?.StrikeoutsPitching.ToString() ?? "0");
            }

            AnsiConsole.Write(table);
        }
    }
}
