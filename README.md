# Baseball CLI

A highly configurable command-line baseball season simulator that uses a hybrid approach combining probability tables with dynamic algorithms to create realistic gameplay.

## Overview

**Baseball CLI** simulates complete baseball seasons with full configurability:
- Define leagues, teams, players, managers, and salaries
- Customize probability tables for hitting, pitching, and fielding
- Simulate games with context-aware algorithms (fatigue, weather, home field advantage)
- Track comprehensive statistics and standings
- Resume saved seasons and explore game-by-game results

## Project Status

**Phase 1-3 Complete (41%)** - 9 of 22 todos implemented

- ✅ Project foundation & database layer
- ✅ Configuration system (JSON-based)
- ✅ Simulation engine (probability tables + algorithms)
- 🔄 Game runner (in progress)
- 📋 CLI interface, stats viewers, testing

## Architecture

### Tech Stack
- **Language**: C# (.NET 6.0)
- **Database**: SQLite with Entity Framework Core
- **CLI Framework**: System.CommandLine + Spectre.Console
- **Configuration**: JSON-based with full validation

### Core Components

**Data Layer** (`Database/`)
- 7-table SQLite schema with relationships
- Repository pattern for data access
- Computed properties (batting average, ERA, standings)

**Domain Models** (`Models/`)
- League, Team, Player, Game, Play, SeasonStats, TeamStats
- Validation and computed properties
- Support for both batting and pitching stats

**Configuration** (`Config/`)
- Full SeasonConfiguration with JSON serialization
- Comprehensive validation
- Example configs with two-team leagues

**Simulation Engine** (`Services/`)
- **ProbabilityTableService**: Batting/pitching/fielding outcomes
- **SimulationAlgorithms**: Fatigue, weather, home advantage, injury logic
- **EventGenerator**: Combines probabilities + algorithms for realistic events

## Building

```bash
dotnet build
```

## Usage (Coming Soon)

```bash
# Create a new season
baseball-cli new

# Load existing season
baseball-cli load <season_id>

# Simulate time period
baseball-cli sim --days 7
baseball-cli sim --week 1
baseball-cli sim --to-end

# Drill into specific game
baseball-cli game --date 2024-04-15

# View standings
baseball-cli standings

# View player stats
baseball-cli stats <player_name>
```

## Configuration

See `Config/example-config.json` for a complete example. You can define:

- League name and description
- Teams with city and manager
- Players with full stats (batting average, power, speed, fielding, salary)
- Game rules (season length, games per series, injury rate, fatigue)
- Probability tables for realistic outcome distributions

```json
{
  "league": {
    "name": "My Baseball League"
  },
  "teams": [
    {
      "name": "Team A",
      "city": "Boston",
      "manager": "Manager Name",
      "players": [...]
    }
  ],
  "rules": {
    "seasonLength": 162,
    "gamesPerSeries": 3,
    "randomSeed": null
  },
  "probabilityTables": {...}
}
```

## Simulation Approach

**Hybrid System**: Combines probability tables with context-aware algorithms

1. **Base Probabilities**: Defines outcome distributions (singles, strikeouts, etc.) by handedness and pitch type
2. **Adjustments**: Algorithms modify probabilities based on:
   - Player stats (batting average increases hit chance)
   - Context (fatigue, season timing, weather, home field)
   - Complex interactions (double plays, sacrifice flies)

3. **Resolution**: Final probability roll determines play outcome

This ensures both realism (algorithm-driven) and tuneability (config-driven).

## Database Schema

```
Leagues
├── Teams (league_id, name, city, manager)
│   ├── Players (team_id, position, stats)
│   │   └── SeasonStats (aggregated stats per season)
│   └── Games (home_team_id, away_team_id, date, score)
│       └── Plays (batter, pitcher, event, result)
└── TeamStats (wins, losses, run differential)
```

## Next Steps

Priority work to enable full gameplay:

1. **game-runner** - Simulate individual games inning-by-inning
2. **season-runner** - Execute season with stat updates
3. **command-structure** - CLI entry point and commands
4. **simulation-controller** - Game flow and pacing control
5. **stats-viewer** - Display results and standings

Then Phase 4 (UI/CLI integration) and Phase 5 (testing/docs).

## Development

Build:
```bash
dotnet build
```

Run:
```bash
dotnet run -- --help
```

Publish:
```bash
dotnet publish -c Release
```

## Features Planned

- ✅ Configurable leagues, teams, players
- ✅ Hybrid simulation engine
- ✅ Database persistence
- 🔄 Single game simulation
- 🔄 Full season simulation
- 📋 CLI commands for all operations
- 📋 Real-time event streaming
- 📋 Drill-down into specific games
- 📋 Comprehensive statistics tracking
- 📋 Unit and integration tests

## License

MIT

