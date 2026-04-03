using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using BaseballCli.Config;
using BaseballCli.Database;
using BaseballCli.Models;
using BaseballCli.Services;

namespace BaseballCli.Commands
{
    /// <summary>
    /// Main CLI entry point and command routing.
    /// </summary>
    public class BaseballCliApp
    {
        private readonly ConfigManager _configManager;
        private readonly BaseballDbContext _dbContext;
        private readonly BaseballRepository _repository;

        public BaseballCliApp(string dbPath = "baseball.db", string configDirectory = "configs")
        {
            _dbContext = new BaseballDbContext(dbPath);
            _repository = new BaseballRepository(_dbContext);
            _configManager = new ConfigManager(configDirectory);
        }

        public async Task<int> RunAsync(string[] args)
        {
            var rootCommand = BuildRootCommand();
            return await rootCommand.InvokeAsync(args);
        }

        private Command BuildRootCommand()
        {
            var root = new RootCommand("Baseball CLI - Season Simulator");

            // Main commands
            root.AddCommand(BuildNewCommand());
            root.AddCommand(BuildLoadCommand());
            root.AddCommand(BuildSimulateCommand());
            root.AddCommand(BuildGameCommand());
            root.AddCommand(BuildStandingsCommand());
            root.AddCommand(BuildStatsCommand());
            root.AddCommand(BuildListCommand());
            root.AddCommand(BuildConfigCommand());

            return root;
        }

        private Command BuildNewCommand()
        {
            var command = new Command("new", "Create a new season configuration");

            var nameOption = new Option<string>(
                new[] { "--name", "-n" },
                "Season name (optional, generates default)"
            );

            var interactiveOption = new Option<bool>(
                new[] { "--interactive", "-i" },
                getDefaultValue: () => false,
                "Use interactive wizard"
            );

            var saveOption = new Option<string>(
                new[] { "--save", "-s" },
                "Save configuration to file with this name"
            );

            command.AddOption(nameOption);
            command.AddOption(interactiveOption);
            command.AddOption(saveOption);

            command.SetHandler((string name, bool interactive, string save) =>
            {
                HandleNewCommand(name, interactive, save);
            }, nameOption, interactiveOption, saveOption);

            return command;
        }

        private void HandleNewCommand(string? name, bool interactive, string? save)
        {
            SeasonConfiguration config;

            if (interactive)
            {
                var wizard = new InteractiveConfigWizard();
                config = wizard.RunWizard();
            }
            else
            {
                config = ConfigLoader.CreateDefaultConfig();
                AnsiConsole.MarkupLine("[green]✓[/] Default configuration created");
            }

            if (name != null)
                config.League.Name = name;

            if (save != null)
            {
                if (_configManager.SaveConfig(save, config))
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] Configuration saved to [bold]{save}.json[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Failed to save configuration");
                }
            }

            DisplayConfigSummary(config);
        }

        private Command BuildLoadCommand()
        {
            var command = new Command("load", "Load an existing season");

            var nameArgument = new Argument<string>(
                "season",
                "Season name or config file name"
            );

            command.AddArgument(nameArgument);

            command.SetHandler((string season) =>
            {
                HandleLoadCommand(season);
            }, nameArgument);

            return command;
        }

        private void HandleLoadCommand(string seasonName)
        {
            var (config, errors) = _configManager.LoadConfig(seasonName);

            if (config == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed to load {seasonName}[/]");
                foreach (var error in errors)
                    AnsiConsole.MarkupLine($"  [red]•[/] {error}");
                return;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Loaded configuration: [bold]{config.League.Name}[/]");
            DisplayConfigSummary(config);
        }

        private Command BuildSimulateCommand()
        {
            var command = new Command("sim", "Simulate games");

            var configOption = new Option<string>(
                new[] { "--config", "-c" },
                "Configuration file name"
            ) { IsRequired = true };

            var daysOption = new Option<int?>(
                new[] { "--days", "-d" },
                "Number of days to simulate"
            );

            var toEndOption = new Option<bool>(
                new[] { "--to-end", "-e" },
                getDefaultValue: () => false,
                "Simulate remaining season"
            );

            var weekOption = new Option<int?>(
                new[] { "--week", "-w" },
                "Number of weeks to simulate"
            );

            command.AddOption(configOption);
            command.AddOption(daysOption);
            command.AddOption(weekOption);
            command.AddOption(toEndOption);

            command.SetHandler((string config, int? days, int? week, bool toEnd) =>
            {
                HandleSimulateCommand(config, days, week, toEnd);
            }, configOption, daysOption, weekOption, toEndOption);

            return command;
        }

        private void HandleSimulateCommand(string configName, int? days, int? weeks, bool toEnd)
        {
            AnsiConsole.MarkupLine("[yellow]Note: Simulation not fully integrated yet[/]");
            AnsiConsole.MarkupLine($"Would simulate: config={configName}, days={days}, weeks={weeks}, toEnd={toEnd}");
        }

        private Command BuildGameCommand()
        {
            var command = new Command("game", "View game details");

            var dateArgument = new Argument<string>(
                "date",
                "Game date (YYYY-MM-DD)"
            );

            var configOption = new Option<string>(
                new[] { "--config", "-c" },
                "Configuration/season name"
            ) { IsRequired = true };

            command.AddArgument(dateArgument);
            command.AddOption(configOption);

            command.SetHandler((string date, string config) =>
            {
                HandleGameCommand(date, config);
            }, dateArgument, configOption);

            return command;
        }

        private void HandleGameCommand(string date, string config)
        {
            AnsiConsole.MarkupLine("[yellow]Note: Game details not fully integrated yet[/]");
            AnsiConsole.MarkupLine($"Would show: config={config}, date={date}");
        }

        private Command BuildStandingsCommand()
        {
            var command = new Command("standings", "Show league standings");

            var configOption = new Option<string>(
                new[] { "--config", "-c" },
                "Configuration/season name"
            ) { IsRequired = true };

            var sortOption = new Option<string>(
                new[] { "--sort", "-s" },
                getDefaultValue: () => "wins",
                "Sort by: wins, losses, runDiff"
            );

            command.AddOption(configOption);
            command.AddOption(sortOption);

            command.SetHandler((string config, string sort) =>
            {
                HandleStandingsCommand(config, sort);
            }, configOption, sortOption);

            return command;
        }

        private void HandleStandingsCommand(string config, string sort)
        {
            try
            {
                var seasonConfig = _configManager.LoadConfig(config);
                var league = _repository.GetLeagueByName(seasonConfig.LeagueName);
                
                if (league == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗ League '{seasonConfig.LeagueName}' not found in database[/]");
                    return;
                }

                var viewer = new StatsViewer(_dbContext);
                viewer.DisplayStandings(league);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error loading standings: {ex.Message}[/]");
            }
        }

        private Command BuildStatsCommand()
        {
            var command = new Command("stats", "Show player statistics");

            var playerArgument = new Argument<string>(
                "player",
                "Player name (optional, shows league leaders if omitted)"
            ) { Arity = ArgumentArity.ZeroOrOne };

            var configOption = new Option<string>(
                new[] { "--config", "-c" },
                "Configuration/season name"
            ) { IsRequired = true };

            command.AddArgument(playerArgument);
            command.AddOption(configOption);

            command.SetHandler((string player, string config) =>
            {
                HandleStatsCommand(player, config);
            }, playerArgument, configOption);

            return command;
        }

        private void HandleStatsCommand(string? player, string config)
        {
            try
            {
                var seasonConfig = _configManager.LoadConfig(config);
                var league = _repository.GetLeagueByName(seasonConfig.LeagueName);
                
                if (league == null)
                {
                    AnsiConsole.MarkupLine($"[red]✗ League '{seasonConfig.LeagueName}' not found in database[/]");
                    return;
                }

                var viewer = new StatsViewer(_dbContext);

                if (string.IsNullOrEmpty(player))
                {
                    // Show league leaders
                    viewer.DisplayLeagueLeaders(league);
                }
                else
                {
                    // Show specific player stats
                    var playerRecord = _repository.GetPlayerByName(player);
                    if (playerRecord == null)
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Player '{player}' not found[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[bold cyan]=== {playerRecord.Name} ===[/]");
                    AnsiConsole.MarkupLine($"[yellow]Position:[/] {playerRecord.Position}");
                    AnsiConsole.MarkupLine($"[yellow]Team:[/] {playerRecord.SeasonStats?.Team?.Name ?? "N/A"}");
                    
                    if (playerRecord.Position == "P")
                    {
                        AnsiConsole.MarkupLine($"[yellow]Record:[/] {playerRecord.SeasonStats?.Wins ?? 0}-{playerRecord.SeasonStats?.Losses ?? 0}");
                        AnsiConsole.MarkupLine($"[yellow]ERA:[/] {(playerRecord.SeasonStats?.ERA ?? 0):F2}");
                        AnsiConsole.MarkupLine($"[yellow]Strikeouts:[/] {playerRecord.SeasonStats?.Strikeouts ?? 0}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]AVG:[/] {(playerRecord.SeasonStats?.BattingAverage ?? 0):F3}");
                        AnsiConsole.MarkupLine($"[yellow]HR:[/] {playerRecord.SeasonStats?.HomeRuns ?? 0}");
                        AnsiConsole.MarkupLine($"[yellow]RBI:[/] {playerRecord.SeasonStats?.RunsBattedIn ?? 0}");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Error loading stats: {ex.Message}[/]");
            }
        }

        private Command BuildListCommand()
        {
            var command = new Command("list", "List available configurations");

            command.SetHandler(() =>
            {
                HandleListCommand();
            });

            return command;
        }

        private void HandleListCommand()
        {
            var configs = _configManager.ListConfigs();

            if (configs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No saved configurations found[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold]Available Configurations:[/]");
            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("Path");

            foreach (var config in configs)
            {
                table.AddRow(config, _configManager.GetConfigPath(config));
            }

            AnsiConsole.Write(table);
        }

        private Command BuildConfigCommand()
        {
            var command = new Command("config", "Manage configurations");

            var deleteCommand = new Command("delete", "Delete a configuration");
            var nameArgument = new Argument<string>("name", "Configuration name");
            deleteCommand.AddArgument(nameArgument);
            deleteCommand.SetHandler((string name) =>
            {
                if (_configManager.DeleteConfig(name))
                    AnsiConsole.MarkupLine($"[green]✓[/] Deleted {name}");
                else
                    AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete {name}");
            }, nameArgument);

            var validateCommand = new Command("validate", "Validate a configuration");
            var configName = new Argument<string>("name", "Configuration name");
            validateCommand.AddArgument(configName);
            validateCommand.SetHandler((string name) =>
            {
                HandleConfigValidateCommand(name);
            }, configName);

            command.AddCommand(deleteCommand);
            command.AddCommand(validateCommand);

            return command;
        }

        private void HandleConfigValidateCommand(string configName)
        {
            var (config, errors) = _configManager.LoadConfig(configName);

            if (config == null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed to load {configName}[/]");
                return;
            }

            var validator = new ConfigurationValidator();
            var result = validator.ValidateConfiguration(config);

            AnsiConsole.WriteLine(result.GetReport());
        }

        private void DisplayConfigSummary(SeasonConfiguration config)
        {
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[bold]Configuration Summary:[/]");

            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("League Name", config.League.Name);
            table.AddRow("Teams", config.Teams.Count.ToString());
            table.AddRow("Total Players", config.Teams.Sum(t => t.Players.Count).ToString());
            table.AddRow("Season Length", config.Rules.SeasonLength.ToString());
            table.AddRow("Games per Series", config.Rules.GamesPerSeries.ToString());
            table.AddRow("Start Date", config.Rules.StartDate);

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("");
        }
    }
}
