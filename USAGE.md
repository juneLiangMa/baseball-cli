# Baseball CLI - Command Line Baseball Season Simulator

A highly configurable command-line application that simulates baseball seasons with realistic probability-based game simulation, comprehensive statistics tracking, and detailed season analysis tools.

## Features

### 🎮 Core Simulation
- **Season Simulation**: Generate and simulate complete baseball seasons
- **Probability-Based Events**: Hybrid approach combining probability tables with player statistics and context
- **Stateful Playback**: Pause and resume seasons at any time
- **Real-Time Viewer**: Watch games with configurable verbosity (minimal/normal/verbose)
- **Configurable Everything**: Customize teams, players, probability tables, and rules

### 📊 Analysis & Viewing
- **League Standings**: Track wins, losses, winning percentage, games behind
- **Player Statistics**: Batting averages, home runs, RBIs, ERA, strikeouts
- **League Leaders**: Top 5 in batting average, home runs, and ERA
- **Game Drill-Down**: Inspect play-by-play details for any game
- **Game Logs**: View recent games for teams or individual players
- **Player Comparison**: Head-to-head statistical comparison

### 🔧 Configuration
- **Interactive Wizard**: Step-by-step setup for new seasons
- **JSON Configuration**: Full control via config files
- **Validation**: Comprehensive config validation before simulation
- **Save/Load**: Create and manage multiple season configurations

### 📈 Statistics
- Batting average calculation
- ERA (Earned Run Average) tracking
- Win-loss records
- Games behind calculation
- Per-game and season-long stats

## Installation

### Requirements
- .NET 6.0 or later
- Cross-platform: Windows, Linux, macOS

### Build from Source
```bash
git clone https://github.com/juneLiangMa/baseball-cli.git
cd baseball-cli
dotnet build -c Release
```

### Using Published Releases
Download pre-built executables from the [Releases page](https://github.com/juneLiangMa/baseball-cli/releases):
- Windows: `baseball-cli-win-x64.exe`
- Linux: `baseball-cli-linux-x64`
- macOS: `baseball-cli-osx-x64` (Intel) or `baseball-cli-osx-arm64` (Apple Silicon)

## Quick Start

### 1. Create a New Season

**Option A: Default Configuration**
```bash
baseball-cli new
```

**Option B: Interactive Wizard**
```bash
baseball-cli new --interactive
```

**Option C: With Custom Name**
```bash
baseball-cli new --name "2024 League" --save my-season
```

### 2. Simulate Games

**Simulate next 7 days**
```bash
baseball-cli sim --days 7 --config my-season
```

**Simulate to end of season**
```bash
baseball-cli sim --to-end --config my-season
```

### 3. View Results

**League Standings**
```bash
baseball-cli standings --config my-season
```

**Player Statistics**
```bash
baseball-cli stats --config my-season              # League leaders
baseball-cli stats "Player Name" --config my-season # Specific player
```

**Game Details**
```bash
baseball-cli game 2024-04-15 --config my-season
```

**Available Configurations**
```bash
baseball-cli list
```

## Usage Examples

### Complete Season Simulation
```bash
# Create a new season with interactive setup
baseball-cli new --interactive --save "Spring 2024"

# Simulate the entire season
baseball-cli sim --to-end --config "Spring 2024"

# View final standings
baseball-cli standings --config "Spring 2024"

# Show league leaders
baseball-cli stats --config "Spring 2024"

# Check a specific player's stats
baseball-cli stats "Mike Trout" --config "Spring 2024"
```

### Multi-Session Simulation
```bash
# Simulate first week
baseball-cli sim --days 7 --config "season2024"

# ... do other things ...

# Continue next week (state is preserved)
baseball-cli sim --days 7 --config "season2024"

# Check progress
baseball-cli standings --config "season2024"
```

### Detailed Game Analysis
```bash
# View all games on a specific date
baseball-cli game 2024-04-15 --config "season2024"

# Inspect specific game details (requires game ID from above)
baseball-cli game 2024-04-15 --config "season2024" --details

# Get team's recent game log
# (Use drill-down interface after simulation)
```

### Configuration Management
```bash
# List all saved configurations
baseball-cli list

# Validate a configuration
baseball-cli config validate --file my-config.json

# Delete a saved configuration
baseball-cli config delete "Old Season"
```

## Configuration Files

### Structure
Create a JSON file to customize your season:

```json
{
  "leagueName": "My League",
  "seasonStart": "2024-04-01",
  "seasonEnd": "2024-10-01",
  "gamesPerSeries": 2,
  "seriesPerTeamPerSeason": 4,
  "rulesConfig": {
    "inningsPerGame": 9,
    "playersPerTeam": 9
  },
  "teams": [
    {
      "name": "Team A",
      "players": [
        {
          "name": "Pitcher 1",
          "position": "P",
          "battingAverage": 0.200
        },
        {
          "name": "Catcher 1",
          "position": "C",
          "battingAverage": 0.280
        }
      ]
    }
  ],
  "outcomeProbabilities": [
    {
      "batterHandedness": "R",
      "pitcherHandedness": "R",
      "pitchType": "Fastball",
      "probabilityMultiplier": 1.0
    }
  ]
}
```

### Teams
Each team requires:
- `name`: Team name (string)
- `players`: Array of players (see below)

### Players
Each player requires:
- `name`: Player name (string)
- `position`: Position (P, C, 1B, 2B, 3B, SS, LF, CF, RF, DH)
- `battingAverage`: Baseline batting average (0.0-1.0)

### Probability Tables
Configure how different batter/pitcher/pitch combinations affect outcomes:
- `batterHandedness`: "L" or "R"
- `pitcherHandedness`: "L" or "R"
- `pitchType`: Type of pitch (e.g., "Fastball", "Slider")
- `probabilityMultiplier`: Adjustment factor (typically 0.5-2.0)

## Command Reference

### `new`
Create a new season configuration
```
Options:
  --name, -n <name>        Season name (generates default if omitted)
  --interactive, -i        Use interactive wizard
  --save, -s <file>        Save configuration to file
```

### `load`
Load and display an existing configuration
```
Arguments:
  <config>                 Configuration/season name
```

### `sim`
Simulate games for a season
```
Options:
  --config, -c <config>    Configuration/season name (required)
  --days <n>               Number of days to simulate
  --to-end                 Simulate to end of season
  --verbosity <level>      minimal, normal, or verbose (default: normal)
```

### `standings`
Display league standings
```
Options:
  --config, -c <config>    Configuration/season name (required)
  --sort, -s <field>       Sort by: wins (default), losses, runDiff
```

### `stats`
Display player statistics
```
Arguments:
  [player]                 Player name (optional, shows league leaders if omitted)

Options:
  --config, -c <config>    Configuration/season name (required)
```

### `game`
View game details
```
Arguments:
  <date>                   Game date (YYYY-MM-DD)

Options:
  --config, -c <config>    Configuration/season name (required)
```

### `list`
List available configurations
```
No options
```

### `config`
Manage configurations
```
Subcommands:
  validate <file>          Validate a configuration file
  delete <name>            Delete a saved configuration
```

## Architecture

### Components

**Domain Models** (`Models/DomainModels.cs`)
- League, Team, Player entities
- Game and Play records
- Statistics tracking

**Database Layer** (`Database/`)
- SQLite persistence via Entity Framework Core
- Repository pattern for data access
- Full relational schema with migrations

**Simulation Engine** (`Services/`)
- `ProbabilityTableService`: Probability resolution
- `EventGenerator`: Play-by-play event generation
- `GameSimulator`: Single game inning-by-inning simulation
- `SeasonSimulator`: Full season orchestration
- `ConfigurationValidator`: Config validation

**CLI Interface** (`Commands/`)
- `BaseballCliApp`: Command routing
- `StatsViewer`: Statistics display
- `RealTimeViewer`: Live game event streaming
- `DrillDownInterface`: Detailed game analysis
- `SimulationController`: State management

**Configuration** (`Config/`)
- `SeasonConfiguration`: Config schema
- `ConfigLoader`: JSON parsing and defaults
- `InteractiveConfigWizard`: Guided setup

### Simulation Algorithm

1. **Schedule Generation**: Creates balanced schedule with round-robin matchups
2. **Game Simulation**: 
   - For each inning, alternates top/bottom
   - For each at-bat: generates event based on probabilities + player stats
   - Tracks runs, outs, base runners
   - Completes after 9 innings
3. **Statistics Accumulation**:
   - Updates player batting averages, home runs, RBIs
   - Calculates ERA for pitchers
   - Tracks team wins and losses
   - Updates standings

## Testing

Run the test suite:

```bash
# Unit tests (core simulation logic)
dotnet test --filter "Category=Unit"

# Integration tests (end-to-end workflows)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## Performance

- Full 162-game season: ~10-30 seconds (depending on verbosity)
- Each game: ~0.1-0.2 seconds
- Database: In-memory SQLite or file-based persistence
- Multi-session support: State saved to JSON between runs

## Customization

### Adjusting Probability

Modify `outcomeProbabilities` in config to change batter-pitcher matchups:
- Higher multiplier = more favorable for batter
- Lower multiplier = more favorable for pitcher

### Changing Player Stats

Edit player `battingAverage` in config to set baseline performance.
The simulation adjusts based on context (fatigue, weather, home field advantage).

### Adjusting Season Length

Modify `seasonStart`, `seasonEnd`, `gamesPerSeries`, and `seriesPerTeamPerSeason`:
```json
"seasonStart": "2024-04-01",
"seasonEnd": "2024-10-01",
"gamesPerSeries": 3,
"seriesPerTeamPerSeason": 2
```

## Known Limitations

- Injury tracking: Not yet implemented
- Weather effects: Simplified (hot = more homers)
- Player trades/free agency: Not supported
- Playoff simulation: Not implemented
- Advanced stats: Limited to basic metrics (BA, ERA, W-L)

## Future Enhancements

- Injury system with recovery tracking
- Trading and free agency
- Playoff simulation
- Advanced statistics (OPS, WHIP, etc.)
- Player vs. pitcher history tracking
- Trade deadline management
- Save/restore from specific dates

## Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

For issues, questions, or suggestions:
- Open an [GitHub Issue](https://github.com/juneLiangMa/baseball-cli/issues)
- Check existing documentation
- Review test cases for usage examples

## Author

Created as a comprehensive baseball simulation engine and CLI tool for .NET.

---

**Ready to simulate?** Start with `baseball-cli new --interactive` to create your first season!
