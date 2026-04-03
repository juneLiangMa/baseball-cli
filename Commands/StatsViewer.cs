using System;
using System.Collections.Generic;
using System.Linq;
using BaseballCli.Database;
using BaseballCli.Models;
using Spectre.Console;

namespace BaseballCli.Commands
{
    public class StatsViewer
    {
        private readonly BaseballDbContext _db;

        public StatsViewer(BaseballDbContext db)
        {
            _db = db;
        }

        public void DisplayStandings(League league)
        {
            var teams = _db.GetTeamsByLeague(league.Id)
                .OrderByDescending(t => t.Wins)
                .ThenByDescending(t => t.Losses)
                .ToList();

            var table = new Table();
            table.Title = new TableTitle($"[bold]{league.Name} Standings[/]");
            table.AddColumn("Team");
            table.AddColumn(new TableColumn("W").Centered());
            table.AddColumn(new TableColumn("L").Centered());
            table.AddColumn(new TableColumn("GB").Centered());
            table.AddColumn(new TableColumn("Win %").Centered());

            double? firstWins = teams.FirstOrDefault()?.Wins;

            foreach (var team in teams)
            {
                var gamesBack = firstWins.HasValue && firstWins > team.Wins
                    ? (firstWins.Value - team.Wins).ToString("F1")
                    : "—";

                var winPct = team.WinPercentage > 0 ? team.WinPercentage.ToString("F3") : ".000";

                table.AddRow(
                    new Text(team.Name),
                    new Text(team.Wins.ToString()).Centered(),
                    new Text(team.Losses.ToString()).Centered(),
                    new Text(gamesBack).Centered(),
                    new Text(winPct).Centered()
                );
            }

            AnsiConsole.Write(table);
        }

        public void DisplayPlayerStats(Team team)
        {
            var players = _db.GetPlayersByTeam(team.Id)
                .Where(p => p.Position != "P")
                .OrderByDescending(p => p.SeasonStats?.BattingAverage ?? 0)
                .ToList();

            var table = new Table();
            table.Title = new TableTitle($"[bold]{team.Name} Player Stats[/]");
            table.AddColumn("Player");
            table.AddColumn("Pos");
            table.AddColumn(new TableColumn("G").Centered());
            table.AddColumn(new TableColumn("AB").Centered());
            table.AddColumn(new TableColumn("H").Centered());
            table.AddColumn(new TableColumn("HR").Centered());
            table.AddColumn(new TableColumn("RBI").Centered());
            table.AddColumn(new TableColumn("AVG").Centered());

            foreach (var player in players)
            {
                var stats = player.SeasonStats;
                var avg = stats?.BattingAverage ?? 0;
                var avgStr = avg > 0 ? avg.ToString("F3") : ".000";

                table.AddRow(
                    player.Name,
                    player.Position,
                    stats?.GamesPlayed.ToString() ?? "0",
                    stats?.AtBats.ToString() ?? "0",
                    stats?.Hits.ToString() ?? "0",
                    stats?.HomeRuns.ToString() ?? "0",
                    stats?.RunsBattedIn.ToString() ?? "0",
                    avgStr
                );
            }

            AnsiConsole.Write(table);
        }

        public void DisplayPitchingStats(Team team)
        {
            var pitchers = _db.GetPlayersByTeam(team.Id)
                .Where(p => p.Position == "P")
                .OrderByDescending(p => p.SeasonStats?.Wins ?? 0)
                .ToList();

            if (!pitchers.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No pitching stats available for {team.Name}[/]");
                return;
            }

            var table = new Table();
            table.Title = new TableTitle($"[bold]{team.Name} Pitching Stats[/]");
            table.AddColumn("Player");
            table.AddColumn(new TableColumn("G").Centered());
            table.AddColumn(new TableColumn("IP").Centered());
            table.AddColumn(new TableColumn("W").Centered());
            table.AddColumn(new TableColumn("L").Centered());
            table.AddColumn(new TableColumn("ERA").Centered());
            table.AddColumn(new TableColumn("SO").Centered());

            foreach (var pitcher in pitchers)
            {
                var stats = pitcher.SeasonStats;
                var eraStr = stats?.ERA ?? 0 > 0 ? (stats?.ERA ?? 0).ToString("F2") : "—";

                table.AddRow(
                    pitcher.Name,
                    stats?.GamesPlayed.ToString() ?? "0",
                    (stats?.InningsPitched ?? 0).ToString("F1"),
                    stats?.Wins.ToString() ?? "0",
                    stats?.Losses.ToString() ?? "0",
                    eraStr,
                    stats?.Strikeouts.ToString() ?? "0"
                );
            }

            AnsiConsole.Write(table);
        }

        public void DisplayLeagueLeaders(League league)
        {
            var allPlayers = _db.GetTeamsByLeague(league.Id)
                .SelectMany(t => _db.GetPlayersByTeam(t.Id))
                .ToList();

            AnsiConsole.MarkupLine("[bold cyan]=== League Leaders ===[/]");
            AnsiConsole.WriteLine();

            // Batting Average Leaders
            var batters = allPlayers.Where(p => p.Position != "P").OrderByDescending(p => p.SeasonStats?.BattingAverage ?? 0).Take(5).ToList();
            if (batters.Any())
            {
                AnsiConsole.MarkupLine("[bold]Batting Average:[/]");
                for (int i = 0; i < batters.Count; i++)
                {
                    var avg = batters[i].SeasonStats?.BattingAverage ?? 0;
                    var avgStr = avg > 0 ? avg.ToString("F3") : ".000";
                    AnsiConsole.MarkupLine($"  {i + 1}. {batters[i].Name} ({batters[i].SeasonStats?.Team?.Name}) - {avgStr}");
                }
                AnsiConsole.WriteLine();
            }

            // Home Run Leaders
            var hrLeaders = allPlayers.Where(p => p.Position != "P").OrderByDescending(p => p.SeasonStats?.HomeRuns ?? 0).Take(5).ToList();
            if (hrLeaders.Any() && (hrLeaders[0].SeasonStats?.HomeRuns ?? 0) > 0)
            {
                AnsiConsole.MarkupLine("[bold]Home Runs:[/]");
                for (int i = 0; i < hrLeaders.Count; i++)
                {
                    AnsiConsole.MarkupLine($"  {i + 1}. {hrLeaders[i].Name} ({hrLeaders[i].SeasonStats?.Team?.Name}) - {hrLeaders[i].SeasonStats?.HomeRuns ?? 0}");
                }
                AnsiConsole.WriteLine();
            }

            // ERA Leaders (pitchers with at least 1 inning pitched)
            var pitchers = allPlayers.Where(p => p.Position == "P" && (p.SeasonStats?.InningsPitched ?? 0) > 0)
                .OrderBy(p => p.SeasonStats?.ERA ?? 0)
                .Take(5)
                .ToList();
            if (pitchers.Any())
            {
                AnsiConsole.MarkupLine("[bold]ERA (min 1 IP):[/]");
                for (int i = 0; i < pitchers.Count; i++)
                {
                    var era = pitchers[i].SeasonStats?.ERA ?? 0;
                    var eraStr = era > 0 ? era.ToString("F2") : "—";
                    AnsiConsole.MarkupLine($"  {i + 1}. {pitchers[i].Name} ({pitchers[i].SeasonStats?.Team?.Name}) - {eraStr}");
                }
            }
        }

        public void DisplaySeasonSummary(League league)
        {
            var teams = _db.GetTeamsByLeague(league.Id).ToList();
            var totalGames = _db.GetGamesByLeague(league.Id).Count();

            AnsiConsole.MarkupLine($"[bold cyan]=== {league.Name} Summary ===[/]");
            AnsiConsole.MarkupLine($"[yellow]Total Games Played:[/] {totalGames}");
            AnsiConsole.MarkupLine($"[yellow]Teams:[/] {teams.Count}");
            
            if (teams.Any())
            {
                // Note: Wins/Losses are on TeamStats, not Team
                AnsiConsole.MarkupLine($"[yellow]Teams:[/] {teams.Count}");
            }
        }
    }
}
