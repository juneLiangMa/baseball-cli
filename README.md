# Baseball CLI

A highly configurable command-line baseball season simulator with a hybrid probability engine, real-time event viewing, and comprehensive statistics tracking.

## Quick Start

```bash
# Create and simulate a season
baseball-cli new --interactive
baseball-cli sim --to-end --config "My Season"
baseball-cli standings --config "My Season"
```

**For detailed usage, see [USAGE.md](USAGE.md)**

## Project Status

**Phase 1-5 Complete (91%)** - 18 of 22 todos done

- ✅ Project foundation & database layer (Phase 1)
- ✅ Configuration system (Phase 2)
- ✅ Simulation engine (Phase 3)
- ✅ CLI interface & stats viewers (Phase 4)
- ✅ Comprehensive test suite (Phase 5 - WIP)

**Remaining:** Refinement and balance tweaking

## Features

### 🎮 Core Simulation
- **Season Simulation**: Generate and simulate complete baseball seasons
- **Probability-Based Events**: Hybrid approach combining probability tables with player stats
- **Stateful Playback**: Pause and resume seasons anytime
- **Real-Time Viewer**: Watch games with configurable verbosity
- **Configurable Everything**: Teams, players, probability tables, rules

### 📊 Analysis Tools
- **League Standings**: W-L records, winning percentage, games behind
- **Player Statistics**: Batting averages, home runs, ERA, strikeouts
- **League Leaders**: Top 5 in key stats
- **Game Drill-Down**: Play-by-play inspection
- **Game Logs**: Recent games for teams or players
- **Player Comparison**: Head-to-head stats

## Building

```bash
dotnet build -c Release
```

For cross-platform publishing:
```bash
dotnet publish -c Release -r win-x64      # Windows
dotnet publish -c Release -r linux-x64    # Linux
dotnet publish -c Release -r osx-x64      # macOS Intel
dotnet publish -c Release -r osx-arm64    # macOS Apple Silicon
```

See [BUILDING.md](BUILDING.md) for detailed build instructions.

## Testing

```bash
# Run all tests
dotnet test

# Or use the test runner (after implementing CLI)
baseball-cli test
```

Tests cover:
- Core probability and event generation
- Full season simulation workflows
- Configuration validation
- Statistics calculations
- Database persistence

## Usage (Coming Soon)

See [USAGE.md](USAGE.md) for complete command reference and examples.

```bash
# Create a new season
baseball-cli new --interactive

# Simulate games
baseball-cli sim --to-end --config "My Season"

# View standings
baseball-cli standings --config "My Season"

# Show player stats
baseball-cli stats --config "My Season"
```

## Configuration

Create a JSON config to customize teams, players, and probability tables:

```json
{
  "leagueName": "My League",
  "seasonStart": "2024-04-01",
  "seasonEnd": "2024-10-01",
  "teams": [
    {
      "name": "Team A",
      "players": [
        { "name": "Player 1", "position": "P", "battingAverage": 0.200 }
      ]
    }
  ]
}
```

For detailed config options, see [USAGE.md](USAGE.md#configuration-files).

## Simulation Approach

**Hybrid System**: Combines probability tables with context-aware adjustments

1. **Base Probabilities**: Outcome distributions by handedness and pitch type
2. **Adjustments**: Modified by player stats and context (fatigue, weather, home field)
3. **Resolution**: Probability roll determines play outcome

Result: Realistic gameplay that's fully configurable and tunable.

## Tech Details

| Component | Details |
|-----------|---------|
| **Simulation** | 15 tests, full season in ~10-30 sec |
| **Database** | 7-table schema, SQLite, EF Core repository pattern |
| **CLI** | System.CommandLine with 8 main commands |
| **Display** | Spectre.Console for rich terminal output |
| **Config** | JSON-based with validation and wizard |

## License

MIT

