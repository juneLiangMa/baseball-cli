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
            // TODO: Standings require TeamStats data for current season
            // For now, just display teams
            var teams = _db.GetTeamsByLeague(league.Id).ToList();

            if (!teams.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No teams found in this league.[/]");
                return;
            }

            var table = new Table();
            table.Title = new TableTitle($"[bold]{league.Name} Teams[/]");
            table.AddColumn("Team");
            table.AddColumn("Manager");
            table.AddColumn("Players");

            foreach (var team in teams)
            {
                table.AddRow(
                    new Text(team.Name),
                    new Text(team.ManagerName),
                    new Text(team.Players?.Count.ToString() ?? "0")
                );
            }

            AnsiConsole.Write(table);
        }

        public void DisplayPlayerStats(Team team)
        {
            var players = _db.GetPlayersByTeam(team.Id)
                .Where(p => p.Position != "P")
                .OrderByDescending(p => p.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.BattingAverage ?? 0)
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
                var stats = player.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();
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
                .OrderByDescending(p => p.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.PitchingWins ?? 0)
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
                var stats = pitcher.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();
                var era = stats?.ERA ?? 0;
                var eraStr = era > 0 ? era.ToString("F2") : "—";

                table.AddRow(
                    pitcher.Name,
                    stats?.GamesPitched.ToString() ?? "0",
                    (stats?.Innings ?? 0).ToString("F1"),
                    stats?.PitchingWins.ToString() ?? "0",
                    stats?.PitchingLosses.ToString() ?? "0",
                    eraStr,
                    stats?.StrikeoutsPitching.ToString() ?? "0"
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
            var batters = allPlayers.Where(p => p.Position != "P").OrderByDescending(p => p.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.BattingAverage ?? 0).Take(5).ToList();
            if (batters.Any())
            {
                AnsiConsole.MarkupLine("[bold]Batting Average:[/]");
                for (int i = 0; i < batters.Count; i++)
                {
                    var stats = batters[i].SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();
                    var avg = stats?.BattingAverage ?? 0;
                    var avgStr = avg > 0 ? avg.ToString("F3") : ".000";
                    AnsiConsole.MarkupLine($"  {i + 1}. {batters[i].Name} ({stats?.Team?.Name}) - {avgStr}");
                }
                AnsiConsole.WriteLine();
            }

            // Home Run Leaders
            var hrLeaders = allPlayers.Where(p => p.Position != "P").OrderByDescending(p => p.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.HomeRuns ?? 0).Take(5).ToList();
            if (hrLeaders.Any() && (hrLeaders[0].SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.HomeRuns ?? 0) > 0)
            {
                AnsiConsole.MarkupLine("[bold]Home Runs:[/]");
                for (int i = 0; i < hrLeaders.Count; i++)
                {
                    var stats = hrLeaders[i].SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();
                    AnsiConsole.MarkupLine($"  {i + 1}. {hrLeaders[i].Name} ({stats?.Team?.Name}) - {stats?.HomeRuns ?? 0}");
                }
                AnsiConsole.WriteLine();
            }

            // ERA Leaders (pitchers with at least 1 inning pitched)
            var pitchers = allPlayers.Where(p => p.Position == "P" && (p.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.Innings ?? 0) > 0)
                .OrderBy(p => p.SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault()?.ERA ?? 0)
                .Take(5)
                .ToList();
            if (pitchers.Any())
            {
                AnsiConsole.MarkupLine("[bold]ERA (min 1 IP):[/]");
                for (int i = 0; i < pitchers.Count; i++)
                {
                    var stats = pitchers[i].SeasonStats?.OrderByDescending(s => s.Season).FirstOrDefault();
                    var era = stats?.ERA ?? 0;
                    var eraStr = era > 0 ? era.ToString("F2") : "—";
                    AnsiConsole.MarkupLine($"  {i + 1}. {pitchers[i].Name} ({stats?.Team?.Name}) - {eraStr}");
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
