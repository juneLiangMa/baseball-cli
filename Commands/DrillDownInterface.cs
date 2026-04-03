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
            var games = _repository.GetGamesByLeague(league.LeagueId);

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
                    ? $"{game.AwayTeamScore}-{game.HomeTeamScore}"
                    : "—";

                var resultColor = game.IsCompleted
                    ? (game.HomeTeamScore > game.AwayTeamScore ? "[green]" : "[red]")
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

            var winner = game.HomeTeamScore > game.AwayTeamScore ? game.HomeTeam : game.AwayTeam;
            var loser = game.HomeTeamScore > game.AwayTeamScore ? game.AwayTeam : game.HomeTeam;

            AnsiConsole.MarkupLine($"[bold cyan]{game.AwayTeam?.Name} @ {game.HomeTeam?.Name}[/]");
            AnsiConsole.MarkupLine($"[yellow]Date:[/] {game.GameDate:yyyy-MM-dd}");
            AnsiConsole.MarkupLine($"[yellow]Result:[/] {winner?.Name} won {Math.Max(game.HomeTeamScore, game.AwayTeamScore)}-{Math.Min(game.HomeTeamScore, game.AwayTeamScore)}");
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
            AnsiConsole.MarkupLine($"[bold]Final: {game.AwayTeam?.Name} {game.AwayTeamScore}, {game.HomeTeam?.Name} {game.HomeTeamScore}[/]");
        }

        public void DisplayTeamGameLog(Team team, int limit = 10)
        {
            var games = _repository.GetGamesByTeam(team.TeamId)
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
                var opponent = game.HomeTeam?.TeamId == team.TeamId ? game.AwayTeam : game.HomeTeam;
                var teamScore = game.HomeTeam?.TeamId == team.TeamId ? game.HomeTeamScore : game.AwayTeamScore;
                var oppScore = game.HomeTeam?.TeamId == team.TeamId ? game.AwayTeamScore : game.HomeTeamScore;

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
            var plays = _repository.GetPlaysByPlayer(player.PlayerId)
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
                var opponent = game?.HomeTeam?.TeamId == player.SeasonStats?.TeamId ? game?.AwayTeam : game?.HomeTeam;

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

            if (player1.Position != "P" && player2.Position != "P")
            {
                // Batting comparison
                table.AddRow("Position", player1.Position, player2.Position);
                table.AddRow("G", player1.SeasonStats?.GamesPlayed.ToString() ?? "0", player2.SeasonStats?.GamesPlayed.ToString() ?? "0");
                table.AddRow("AB", player1.SeasonStats?.AtBats.ToString() ?? "0", player2.SeasonStats?.AtBats.ToString() ?? "0");
                table.AddRow("H", player1.SeasonStats?.Hits.ToString() ?? "0", player2.SeasonStats?.Hits.ToString() ?? "0");
                table.AddRow("AVG", (player1.SeasonStats?.BattingAverage ?? 0).ToString("F3"), (player2.SeasonStats?.BattingAverage ?? 0).ToString("F3"));
                table.AddRow("HR", player1.SeasonStats?.HomeRuns.ToString() ?? "0", player2.SeasonStats?.HomeRuns.ToString() ?? "0");
                table.AddRow("RBI", player1.SeasonStats?.RunsBattedIn.ToString() ?? "0", player2.SeasonStats?.RunsBattedIn.ToString() ?? "0");
            }
            else if (player1.Position == "P" && player2.Position == "P")
            {
                // Pitching comparison
                table.AddRow("G", player1.SeasonStats?.GamesPlayed.ToString() ?? "0", player2.SeasonStats?.GamesPlayed.ToString() ?? "0");
                table.AddRow("W-L", $"{player1.SeasonStats?.Wins ?? 0}-{player1.SeasonStats?.Losses ?? 0}", 
                    $"{player2.SeasonStats?.Wins ?? 0}-{player2.SeasonStats?.Losses ?? 0}");
                table.AddRow("ERA", (player1.SeasonStats?.ERA ?? 0).ToString("F2"), (player2.SeasonStats?.ERA ?? 0).ToString("F2"));
                table.AddRow("IP", (player1.SeasonStats?.InningsPitched ?? 0).ToString("F1"), (player2.SeasonStats?.InningsPitched ?? 0).ToString("F1"));
                table.AddRow("SO", player1.SeasonStats?.Strikeouts.ToString() ?? "0", player2.SeasonStats?.Strikeouts.ToString() ?? "0");
            }

            AnsiConsole.Write(table);
        }
    }
}
