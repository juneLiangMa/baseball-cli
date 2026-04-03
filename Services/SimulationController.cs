using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BaseballCli.Models;
using Spectre.Console;

namespace BaseballCli.Services
{
    /// <summary>
    /// Manages the state of an ongoing season simulation.
    /// Tracks which games have been played, current date, and allows resuming later.
    /// </summary>
    public class SimulationController
    {
        public class SimulationState
        {
            public string LeagueName { get; set; }
            public string LeagueId { get; set; }
            public DateTime CurrentDate { get; set; }
            public DateTime SeasonStart { get; set; }
            public DateTime SeasonEnd { get; set; }
            public List<string> CompletedGameIds { get; set; } = new();
            public int GamesSimulated { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private readonly string _stateDirectory;
        private SimulationState _currentState;
        private bool _isLoaded;

        public SimulationController(string stateDirectory = ".simulation-state")
        {
            _stateDirectory = stateDirectory;
            if (!Directory.Exists(_stateDirectory))
            {
                Directory.CreateDirectory(_stateDirectory);
            }
            _isLoaded = false;
        }

        public bool TryLoadState(string leagueName)
        {
            try
            {
                var statePath = GetStatePath(leagueName);
                if (!File.Exists(statePath))
                {
                    return false;
                }

                var json = File.ReadAllText(statePath);
                _currentState = JsonSerializer.Deserialize<SimulationState>(json);
                _isLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed to load simulation state: {ex.Message}[/]");
                return false;
            }
        }

        public void InitializeNewSimulation(League league, DateTime seasonStart, DateTime seasonEnd)
        {
            _currentState = new SimulationState
            {
                LeagueName = league.Name,
                LeagueId = league.Id,
                CurrentDate = seasonStart,
                SeasonStart = seasonStart,
                SeasonEnd = seasonEnd,
                CompletedGameIds = new(),
                GamesSimulated = 0,
                LastUpdated = DateTime.Now
            };
            _isLoaded = true;
            SaveState();
        }

        public void AdvanceSimulation(int daysToSimulate, int gamesCompleted)
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("No simulation loaded");
            }

            _currentState.CurrentDate = _currentState.CurrentDate.AddDays(daysToSimulate);
            _currentState.GamesSimulated += gamesCompleted;
            _currentState.LastUpdated = DateTime.Now;
            SaveState();
        }

        public void RecordGameCompletion(string gameId)
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("No simulation loaded");
            }

            if (!_currentState.CompletedGameIds.Contains(gameId))
            {
                _currentState.CompletedGameIds.Add(gameId);
                _currentState.GamesSimulated++;
                _currentState.LastUpdated = DateTime.Now;
                SaveState();
            }
        }

        public void SaveState()
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("No simulation to save");
            }

            try
            {
                var statePath = GetStatePath(_currentState.LeagueName);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_currentState, options);
                File.WriteAllText(statePath, json);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed to save simulation state: {ex.Message}[/]");
            }
        }

        public void DisplaySimulationProgress()
        {
            if (!_isLoaded)
            {
                AnsiConsole.MarkupLine("[yellow]No active simulation[/]");
                return;
            }

            var daysRemaining = (_currentState.SeasonEnd - _currentState.CurrentDate).Days;
            var totalDays = (_currentState.SeasonEnd - _currentState.SeasonStart).Days;
            var percentComplete = (totalDays - daysRemaining) / (double)totalDays * 100;

            var panel = new Panel($"""
                [bold]{_currentState.LeagueName} Simulation Progress[/]
                
                [yellow]Current Date:[/] {_currentState.CurrentDate:yyyy-MM-dd}
                [yellow]Season End:[/] {_currentState.SeasonEnd:yyyy-MM-dd}
                [yellow]Days Remaining:[/] {daysRemaining}
                [yellow]Games Simulated:[/] {_currentState.GamesSimulated}
                [yellow]Completion:[/] {percentComplete:F1}%
                [yellow]Last Updated:[/] {_currentState.LastUpdated:yyyy-MM-dd HH:mm:ss}
                """);

            panel.BorderColor(Color.Cyan1);
            AnsiConsole.Write(panel);
        }

        public bool IsSeasonComplete()
        {
            if (!_isLoaded)
                return false;

            return _currentState.CurrentDate >= _currentState.SeasonEnd;
        }

        public DateTime GetCurrentDate()
        {
            if (!_isLoaded)
                throw new InvalidOperationException("No simulation loaded");
            return _currentState.CurrentDate;
        }

        public int GetGamesSimulated()
        {
            if (!_isLoaded)
                throw new InvalidOperationException("No simulation loaded");
            return _currentState.GamesSimulated;
        }

        public SimulationState GetCurrentState()
        {
            if (!_isLoaded)
                throw new InvalidOperationException("No simulation loaded");
            return _currentState;
        }

        public void DeleteState(string leagueName)
        {
            var statePath = GetStatePath(leagueName);
            if (File.Exists(statePath))
            {
                File.Delete(statePath);
            }
        }

        public List<string> GetAvailableSimulations()
        {
            try
            {
                var files = Directory.GetFiles(_stateDirectory, "*.json");
                return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
            }
            catch
            {
                return new();
            }
        }

        private string GetStatePath(string leagueName)
        {
            return Path.Combine(_stateDirectory, $"{leagueName}.json");
        }
    }
}
