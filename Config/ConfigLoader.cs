using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaseballCli.Config
{
    /// <summary>
    /// Loads and validates season configuration from JSON files.
    /// </summary>
    public class ConfigLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Loads a season configuration from a JSON file.
        /// </summary>
        public static async Task<(SeasonConfiguration? config, List<string> errors)> LoadConfigAsync(string filePath)
        {
            var errors = new List<string>();

            if (!File.Exists(filePath))
            {
                errors.Add($"Configuration file not found: {filePath}");
                return (null, errors);
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<SeasonConfiguration>(json, JsonOptions);

                if (config == null)
                {
                    errors.Add("Failed to deserialize configuration file");
                    return (null, errors);
                }

                // Validate the configuration
                var validationErrors = config.Validate();
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors);
                    return (config, errors);
                }

                return (config, errors);
            }
            catch (JsonException jsonEx)
            {
                errors.Add($"JSON parsing error: {jsonEx.Message}");
                return (null, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading configuration file: {ex.Message}");
                return (null, errors);
            }
        }

        /// <summary>
        /// Loads a season configuration synchronously.
        /// </summary>
        public static (SeasonConfiguration? config, List<string> errors) LoadConfig(string filePath)
        {
            var errors = new List<string>();

            if (!File.Exists(filePath))
            {
                errors.Add($"Configuration file not found: {filePath}");
                return (null, errors);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<SeasonConfiguration>(json, JsonOptions);

                if (config == null)
                {
                    errors.Add("Failed to deserialize configuration file");
                    return (null, errors);
                }

                // Validate the configuration
                var validationErrors = config.Validate();
                if (validationErrors.Count > 0)
                {
                    errors.AddRange(validationErrors);
                    return (config, errors);
                }

                return (config, errors);
            }
            catch (JsonException jsonEx)
            {
                errors.Add($"JSON parsing error: {jsonEx.Message}");
                return (null, errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Error reading configuration file: {ex.Message}");
                return (null, errors);
            }
        }

        /// <summary>
        /// Saves a season configuration to a JSON file.
        /// </summary>
        public static async Task<bool> SaveConfigAsync(string filePath, SeasonConfiguration config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves a season configuration synchronously.
        /// </summary>
        public static bool SaveConfig(string filePath, SeasonConfiguration config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a default/example configuration.
        /// </summary>
        public static SeasonConfiguration CreateDefaultConfig()
        {
            var config = new SeasonConfiguration
            {
                League = new LeagueConfig
                {
                    Name = "Example League",
                    Description = "A simple example baseball league"
                },
                Teams = new List<TeamConfig>
                {
                    new()
                    {
                        Name = "Team A",
                        City = "City A",
                        Manager = "Manager A",
                        Players = CreateDefaultRoster("Team A")
                    },
                    new()
                    {
                        Name = "Team B",
                        City = "City B",
                        Manager = "Manager B",
                        Players = CreateDefaultRoster("Team B")
                    }
                },
                Rules = new RulesConfig
                {
                    SeasonLength = 162,
                    GamesPerSeries = 3,
                    InningsPerGame = 9,
                    RandomSeed = null,
                    InjuryRate = 0.02,
                    FatigueThreshold = 5,
                    StartDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd")
                },
                ProbabilityTables = new ProbabilityTablesConfig()
            };

            return config;
        }

        private static List<PlayerConfig> CreateDefaultRoster(string teamName)
        {
            var positions = new[] { "Pitcher", "Pitcher", "Catcher", "Infielder", "Infielder", "Infielder", "Infielder", "Outfielder", "Outfielder" };
            var players = new List<PlayerConfig>();

            for (int i = 0; i < 9; i++)
            {
                var isPitcher = positions[i] == "Pitcher";
                players.Add(new PlayerConfig
                {
                    Name = $"{teamName} Player {i + 1}",
                    Gender = "M",
                    Position = positions[i],
                    BattingAverage = isPitcher ? 0.150 : 0.280,
                    PowerRating = 0.400,
                    SpeedRating = 0.400,
                    FieldingAverage = 0.950,
                    PitchingSpeed = isPitcher ? 90 : null,
                    ControlRating = isPitcher ? 0.75 : null,
                    Salary = 2000000
                });
            }

            return players;
        }

        /// <summary>
        /// Validates a configuration without loading from file.
        /// </summary>
        public static List<string> ValidateConfig(SeasonConfiguration config)
        {
            return config.Validate();
        }
    }

    /// <summary>
    /// Manager for handling multiple season configurations.
    /// </summary>
    public class ConfigManager
    {
        private readonly string _configDirectory;

        public ConfigManager(string configDirectory = "configs")
        {
            _configDirectory = configDirectory;
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        /// <summary>
        /// Gets the path to a configuration file by name.
        /// </summary>
        public string GetConfigPath(string configName)
        {
            return Path.Combine(_configDirectory, $"{configName}.json");
        }

        /// <summary>
        /// Lists all available configuration files.
        /// </summary>
        public List<string> ListConfigs()
        {
            if (!Directory.Exists(_configDirectory))
                return new List<string>();

            var files = Directory.GetFiles(_configDirectory, "*.json");
            return new List<string>(files.Select(f => Path.GetFileNameWithoutExtension(f)));
        }

        /// <summary>
        /// Loads a configuration by name.
        /// </summary>
        public (SeasonConfiguration? config, List<string> errors) LoadConfig(string configName)
        {
            return ConfigLoader.LoadConfig(GetConfigPath(configName));
        }

        /// <summary>
        /// Saves a configuration by name.
        /// </summary>
        public bool SaveConfig(string configName, SeasonConfiguration config)
        {
            return ConfigLoader.SaveConfig(GetConfigPath(configName), config);
        }

        /// <summary>
        /// Deletes a configuration file.
        /// </summary>
        public bool DeleteConfig(string configName)
        {
            try
            {
                var path = GetConfigPath(configName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting configuration: {ex.Message}");
                return false;
            }
        }
    }
}
