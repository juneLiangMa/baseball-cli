using System;
using System.Threading;
using System.Threading.Tasks;
using BaseballCli.Models;
using Spectre.Console;

namespace BaseballCli.Commands
{
    /// <summary>
    /// Displays game events in real-time with formatting and pacing for a "live" feel.
    /// Supports different verbosity levels.
    /// </summary>
    public class RealTimeViewer
    {
        public enum Verbosity
        {
            Minimal,      // Only runs/key plays
            Normal,       // Inning summaries
            Verbose       // Play-by-play
        }

        private readonly Verbosity _verbosity;
        private int _playDelayMs;
        private int _inningDelayMs;

        public RealTimeViewer(Verbosity verbosity = Verbosity.Normal, int playDelayMs = 500, int inningDelayMs = 1000)
        {
            _verbosity = verbosity;
            _playDelayMs = playDelayMs;
            _inningDelayMs = inningDelayMs;
        }

        public void DisplayGameStart(Team homeTeam, Team awayTeam, DateTime gameDate)
        {
            var header = new Panel($"""
                [bold cyan]{awayTeam.Name} @ {homeTeam.Name}[/]
                [dim]{gameDate:yyyy-MM-dd}[/]
                """);
            header.BorderColor(Color.Cyan1);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();
        }

        public void DisplayInningStart(int inning, bool isTop)
        {
            var period = isTop ? "Top" : "Bottom";
            var text = new Markup($"[bold yellow]═══ {period} of Inning {inning} ═══[/]");
            AnsiConsole.Write(text);
            AnsiConsole.WriteLine();
        }

        public void DisplayAtBat(Player batter, Player pitcher, string result)
        {
            if (_verbosity == Verbosity.Minimal)
                return;

            var text = $"[green]{batter.Name}[/] vs [cyan]{pitcher.Name}[/]: [yellow]{result}[/]";
            AnsiConsole.MarkupLine(text);

            if (_verbosity == Verbosity.Verbose)
            {
                Thread.Sleep(_playDelayMs);
            }
        }

        public void DisplayPlay(Play play)
        {
            if (_verbosity == Verbosity.Minimal && !IsKeyPlay(play))
                return;

            var eventColor = GetEventColor(play.EventType);
            var text = $"[{eventColor}]{play.EventType}[/]";

            if (!string.IsNullOrEmpty(play.EventDetails))
            {
                text += $" - {play.EventDetails}";
            }

            AnsiConsole.MarkupLine(text);

            if (_verbosity == Verbosity.Verbose)
            {
                Thread.Sleep(_playDelayMs);
            }
        }

        public void DisplayRun(Team scoringTeam, int runCount)
        {
            var text = $"[bold green]★ {scoringTeam.Name} scores {runCount} run{(runCount > 1 ? "s" : "")}![/]";
            AnsiConsole.MarkupLine(text);
            Thread.Sleep(_playDelayMs * 2);
        }

        public void DisplayInningScore(int inning, Team homeTeam, Team awayTeam, int homeScore, int awayScore)
        {
            var padding = Math.Max(homeTeam.Name.Length, awayTeam.Name.Length) + 2;
            var format = $"[yellow]{{0,{padding}}}[/] [cyan]I{inning}[/]: {{1}}";

            AnsiConsole.MarkupLine(string.Format(format, awayTeam.Name, awayScore));
            AnsiConsole.MarkupLine(string.Format(format, homeTeam.Name, homeScore));
            AnsiConsole.WriteLine();

            Thread.Sleep(_inningDelayMs);
        }

        public void DisplayGameEnd(Game game, Team homeTeam, Team awayTeam)
        {
            var winner = game.HomeScore > game.AwayScore ? homeTeam : awayTeam;
            var loser = game.HomeScore > game.AwayScore ? awayTeam : homeTeam;
            var winScore = game.HomeScore > game.AwayScore ? game.HomeScore : game.AwayScore;
            var loseScore = game.HomeScore > game.AwayScore ? game.AwayScore : game.HomeScore;

            AnsiConsole.WriteLine();
            var footer = new Panel($"""
                [bold green]✓ Game Final[/]
                
                [bold]{winner.Name}[/] [green]{winScore}[/], [bold]{loser.Name}[/] [red]{loseScore}[/]
                """);
            footer.BorderColor(Color.Green1);
            AnsiConsole.Write(footer);
        }

        public void DisplayBriefInningScore(int inning, int homeRuns, int awayRuns)
        {
            if (_verbosity == Verbosity.Minimal)
            {
                AnsiConsole.MarkupLine($"[yellow]I{inning}:[/] {awayRuns}-{homeRuns}");
            }
        }

        private static bool IsKeyPlay(Play play)
        {
            return play.EventType switch
            {
                "HomeRun" => true,
                "Triple" => true,
                "DoublePlay" => true,
                "StrikeoutLooking" => true,
                "Error" => true,
                "CaughtStealing" => true,
                "StolenBase" => true,
                _ => false
            };
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
                "StrikeoutLooking" => "bold red",
                "Out" => "red",
                "DoublePlay" => "bold red",
                "TriplePlay" => "bold red",
                "Error" => "yellow",
                "StolenBase" => "blue",
                "CaughtStealing" => "bold red",
                "FieldersChoice" => "yellow",
                _ => "white"
            };
        }

        public void SetPlaySpeed(double speedMultiplier)
        {
            _playDelayMs = (int)(500 / speedMultiplier);
            _inningDelayMs = (int)(1000 / speedMultiplier);
        }

        public async Task DisplayWithProgress(Func<IProgress<GameProgressMessage>, Task> simulationFunc)
        {
            var progress = new Progress<GameProgressMessage>(message =>
            {
                switch (message.MessageType)
                {
                    case GameProgressMessageType.InningStart:
                        DisplayInningStart(message.Inning, message.IsTop);
                        break;
                    case GameProgressMessageType.Play:
                        if (message.Play != null)
                            DisplayPlay(message.Play);
                        break;
                    case GameProgressMessageType.Run:
                        if (message.Team != null)
                            DisplayRun(message.Team, message.RunCount);
                        break;
                    case GameProgressMessageType.InningEnd:
                        if (message.HomeTeam != null && message.AwayTeam != null)
                            DisplayInningScore(message.Inning, message.HomeTeam, message.AwayTeam, 
                                message.HomeScore, message.AwayScore);
                        break;
                }
            });

            await simulationFunc(progress);
        }
    }

    public enum GameProgressMessageType
    {
        InningStart,
        Play,
        Run,
        InningEnd
    }

    public class GameProgressMessage
    {
        public GameProgressMessageType MessageType { get; set; }
        public int Inning { get; set; }
        public bool IsTop { get; set; }
        public Play Play { get; set; }
        public Team Team { get; set; }
        public int RunCount { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
    }
}
