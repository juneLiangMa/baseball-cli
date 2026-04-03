using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using BaseballCli.Config;

namespace BaseballCli.Services
{
    /// <summary>
    /// Interactive CLI wizard for creating baseball season configurations.
    /// Provides step-by-step prompts for all configuration elements.
    /// </summary>
    public class InteractiveConfigWizard
    {
        private readonly ConfigurationValidator _validator = new();

        public SeasonConfiguration RunWizard()
        {
            AnsiConsole.MarkupLine("[bold cyan]🏟️  Baseball CLI - Season Configuration Wizard[/]");
            AnsiConsole.MarkupLine("[dim]Create a new baseball season configuration interactively[/]\n");

            var config = new SeasonConfiguration();

            config.League = CreateLeague();
            config.Teams = CreateTeams();
            config.Rules = CreateRules();
            config.ProbabilityTables = CreateProbabilityTables();

            // Validate before returning
            var validation = _validator.ValidateConfiguration(config);
            if (!validation.IsValid)
            {
                AnsiConsole.MarkupLine("[bold red]❌ Configuration has errors:[/]");
                AnsiConsole.WriteLine(validation.GetReport(false));

                if (AnsiConsole.Confirm("[yellow]Continue anyway?[/]"))
                    return config;
                else
                    throw new InvalidOperationException("Configuration validation failed");
            }

            if (validation.HasWarnings)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️  Configuration warnings:[/]");
                foreach (var warning in validation.Warnings)
                    AnsiConsole.MarkupLine($"  [yellow]•[/] {warning}");

                if (!AnsiConsole.Confirm("[yellow]Continue?[/]"))
                    return RunWizard(); // Start over
            }

            AnsiConsole.MarkupLine("[green]✅ Configuration created successfully![/]\n");
            return config;
        }

        private LeagueConfig CreateLeague()
        {
            AnsiConsole.MarkupLine("[bold]League Setup[/]");

            var league = new LeagueConfig
            {
                Name = AnsiConsole.Ask<string>("League name: ", "Major Baseball League"),
                Description = AnsiConsole.Ask<string>("Description (optional): ", "")
            };

            AnsiConsole.MarkupLine("");
            return league;
        }

        private List<TeamConfig> CreateTeams()
        {
            AnsiConsole.MarkupLine("[bold]Teams Setup[/]");

            int numTeams = AnsiConsole.Ask<int>("How many teams? ", 2);
            if (numTeams < 1 || numTeams > 50)
                numTeams = Math.Max(1, Math.Min(50, numTeams));

            var teams = new List<TeamConfig>();

            for (int i = 0; i < numTeams; i++)
            {
                AnsiConsole.MarkupLine($"\n[cyan]Team {i + 1}[/]");
                teams.Add(CreateTeam());
            }

            AnsiConsole.MarkupLine("");
            return teams;
        }

        private TeamConfig CreateTeam()
        {
            var team = new TeamConfig
            {
                Name = AnsiConsole.Ask<string>("  Team name: "),
                City = AnsiConsole.Ask<string>("  City: "),
                Manager = AnsiConsole.Ask<string>("  Manager name: "),
                Players = new List<PlayerConfig>()
            };

            int numPlayers = AnsiConsole.Ask<int>("  How many players? (minimum 9): ", 9);
            numPlayers = Math.Max(9, numPlayers);

            for (int i = 0; i < numPlayers; i++)
            {
                team.Players.Add(CreatePlayer(i + 1, numPlayers));
            }

            return team;
        }

        private PlayerConfig CreatePlayer(int playerNum, int totalPlayers)
        {
            // Auto-generate pitchers at the beginning
            bool isPitcher = playerNum <= 2 || (totalPlayers > 20 && playerNum <= 5);
            
            var player = new PlayerConfig();

            string prompt = $"    Player {playerNum}";
            player.Name = AnsiConsole.Ask<string>($"{prompt} name: ");

            if (AnsiConsole.Confirm($"{prompt} is pitcher?", isPitcher))
            {
                player.Position = "Pitcher";
                player.BattingAverage = 0.150;
                player.PowerRating = 0.200;
                player.SpeedRating = 0.300;
                player.FieldingAverage = 0.950;
                player.PitchingSpeed = AnsiConsole.Ask<double>($"{prompt} pitching speed (70-105 mph): ", 90);
                player.ControlRating = AnsiConsole.Ask<double>($"{prompt} control rating (0-1): ", 0.75);
            }
            else
            {
                var positions = new[] { "Catcher", "Infielder", "Outfielder" };
                player.Position = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"{prompt} position:")
                        .AddChoices(positions)
                );

                player.BattingAverage = AnsiConsole.Ask<double>($"{prompt} batting average (0.150-0.350): ", 0.280);
                player.PowerRating = AnsiConsole.Ask<double>($"{prompt} power rating (0-1): ", 0.400);
                player.SpeedRating = AnsiConsole.Ask<double>($"{prompt} speed rating (0-1): ", 0.400);
                player.FieldingAverage = AnsiConsole.Ask<double>($"{prompt} fielding average (0.85-1.00): ", 0.950);
            }

            player.Gender = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"{prompt} gender:")
                    .AddChoices("M", "F")
            );

            player.Salary = AnsiConsole.Ask<double>($"{prompt} salary (in millions): ", 2) * 1000000;

            return player;
        }

        private RulesConfig CreateRules()
        {
            AnsiConsole.MarkupLine("\n[bold]Game Rules[/]");

            bool useDefaults = AnsiConsole.Confirm("Use default rules?", true);

            if (useDefaults)
                return new RulesConfig();

            var rules = new RulesConfig
            {
                SeasonLength = AnsiConsole.Ask<int>("Season length (games): ", 162),
                GamesPerSeries = AnsiConsole.Ask<int>("Games per series: ", 3),
                InningsPerGame = AnsiConsole.Ask<int>("Innings per game: ", 9),
                InjuryRate = AnsiConsole.Ask<double>("Injury rate (0.00-0.05): ", 0.02),
                FatigueThreshold = AnsiConsole.Ask<int>("Fatigue threshold (games): ", 5),
                StartDate = AnsiConsole.Ask<string>("Start date (YYYY-MM-DD): ", DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd")),
                RandomSeed = AnsiConsole.Ask<int?>("Random seed (optional, press Enter to skip): ", null)
            };

            AnsiConsole.MarkupLine("");
            return rules;
        }

        private ProbabilityTablesConfig CreateProbabilityTables()
        {
            AnsiConsole.MarkupLine("[bold]Probability Tables[/]");

            bool useDefaults = AnsiConsole.Confirm("Use default probability tables?", true);

            if (useDefaults)
                return new ProbabilityTablesConfig();

            AnsiConsole.MarkupLine("[yellow]Entering custom probability tables configuration...[/]");
            AnsiConsole.MarkupLine("[dim]Note: Each set of probabilities must sum to 1.0[/]\n");

            var tables = new ProbabilityTablesConfig();

            // Batting
            AnsiConsole.MarkupLine("[cyan]Batting Outcomes (RHH vs RHP):[/]");
            tables.Batting.RightVsRight = CreateOutcomeProbabilities("RVR");

            AnsiConsole.MarkupLine("[cyan]Batting Outcomes (RHH vs LHP):[/]");
            tables.Batting.RightVsLeft = CreateOutcomeProbabilities("RVL");

            AnsiConsole.MarkupLine("[cyan]Batting Outcomes (LHH vs RHP):[/]");
            tables.Batting.LeftVsRight = CreateOutcomeProbabilities("LVR");

            AnsiConsole.MarkupLine("[cyan]Batting Outcomes (LHH vs LHP):[/]");
            tables.Batting.LeftVsLeft = CreateOutcomeProbabilities("LVL");

            AnsiConsole.MarkupLine("");

            // Pitching
            AnsiConsole.MarkupLine("[cyan]Pitching Outcomes (Fastball):[/]");
            tables.Pitching.Fastball = CreateOutcomeProbabilities("FB");

            AnsiConsole.MarkupLine("[cyan]Pitching Outcomes (Offspeed):[/]");
            tables.Pitching.Offspeed = CreateOutcomeProbabilities("OS");

            AnsiConsole.MarkupLine("");

            // Fielding
            AnsiConsole.MarkupLine("[cyan]Fielding Success Rates:[/]");
            double infielderOut = AnsiConsole.Ask<double>("Infielder out rate (0.90-0.99): ", 0.965);
            tables.Fielding.Infielder = new FieldingOutcomes 
            { 
                Out = infielderOut, 
                Error = 1.0 - infielderOut 
            };

            double outfielderOut = AnsiConsole.Ask<double>("Outfielder out rate (0.90-0.99): ", 0.950);
            tables.Fielding.Outfielder = new FieldingOutcomes 
            { 
                Out = outfielderOut, 
                Error = 1.0 - outfielderOut 
            };

            AnsiConsole.MarkupLine("");
            return tables;
        }

        private OutcomeProabilities CreateOutcomeProbabilities(string context)
        {
            var probs = new OutcomeProabilities();

            AnsiConsole.MarkupLine($"[dim]Current defaults for {context}:[/]");
            AnsiConsole.MarkupLine($"  Strikeout: {probs.Strikeout:P1}, Walk: {probs.Walk:P1}, Single: {probs.Single:P1}");
            AnsiConsole.MarkupLine($"  Double: {probs.Double:P1}, Triple: {probs.Triple:P1}, HR: {probs.HomeRun:P1}");
            AnsiConsole.MarkupLine($"  FC: {probs.FieldersChoice:P1}, Out: {probs.Out:P1}");

            if (AnsiConsole.Confirm("Modify these probabilities?", false))
            {
                probs.Strikeout = AnsiConsole.Ask<double>("  Strikeout rate: ", probs.Strikeout);
                probs.Walk = AnsiConsole.Ask<double>("  Walk rate: ", probs.Walk);
                probs.Single = AnsiConsole.Ask<double>("  Single rate: ", probs.Single);
                probs.Double = AnsiConsole.Ask<double>("  Double rate: ", probs.Double);
                probs.Triple = AnsiConsole.Ask<double>("  Triple rate: ", probs.Triple);
                probs.HomeRun = AnsiConsole.Ask<double>("  Home run rate: ", probs.HomeRun);
                probs.FieldersChoice = AnsiConsole.Ask<double>("  Fielders choice rate: ", probs.FieldersChoice);
                probs.Out = AnsiConsole.Ask<double>("  Out rate: ", probs.Out);

                // Auto-normalize
                double sum = probs.Strikeout + probs.Walk + probs.Single + probs.Double +
                            probs.Triple + probs.HomeRun + probs.FieldersChoice + probs.Out;

                if (Math.Abs(sum - 1.0) > 0.001)
                {
                    AnsiConsole.MarkupLine($"[yellow]Note: Probabilities sum to {sum:F3}, normalizing to 1.0[/]");
                    probs.Strikeout /= sum;
                    probs.Walk /= sum;
                    probs.Single /= sum;
                    probs.Double /= sum;
                    probs.Triple /= sum;
                    probs.HomeRun /= sum;
                    probs.FieldersChoice /= sum;
                    probs.Out /= sum;
                }
            }

            AnsiConsole.MarkupLine("");
            return probs;
        }
    }

    /// <summary>
    /// Helper for interactive configuration workflows.
    /// </summary>
    public class ConfigWizardHelper
    {
        /// <summary>
        /// Prompts user to choose between creating new or loading existing config.
        /// </summary>
        public static ConfigChoice PromptConfigChoice()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<ConfigChoice>()
                    .Title("What would you like to do?")
                    .AddChoices(
                        ConfigChoice.CreateNew,
                        ConfigChoice.LoadExisting,
                        ConfigChoice.Cancel
                    )
            );
        }

        /// <summary>
        /// Prompts for file path to save configuration.
        /// </summary>
        public static string PromptSavePath(string defaultName = "season")
        {
            return AnsiConsole.Ask<string>("Configuration filename (without .json): ", defaultName);
        }

        /// <summary>
        /// Prompts for file path to load configuration from.
        /// </summary>
        public static string PromptLoadPath(IEnumerable<string> availableConfigs)
        {
            var configs = availableConfigs.ToList();
            
            if (configs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No saved configurations found[/]");
                return AnsiConsole.Ask<string>("Enter configuration filename (without .json): ");
            }

            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a configuration:")
                    .AddChoices(configs)
            );
        }
    }

    public enum ConfigChoice
    {
        [System.ComponentModel.Description("Create New Configuration")]
        CreateNew,

        [System.ComponentModel.Description("Load Existing Configuration")]
        LoadExisting,

        [System.ComponentModel.Description("Cancel")]
        Cancel
    }
}
