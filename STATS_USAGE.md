# Stats Accumulator Usage Guide

The `StatsAccumulator` service provides a clean API for tracking baseball statistics during game simulation.

## Basic Usage

### Initialize

```csharp
var dbContext = new BaseballDbContext("baseball.db");
var repository = new BaseballRepository(dbContext);
var statsAccumulator = new StatsAccumulator(repository);
```

### Initialize Season Stats

```csharp
// Initialize all players' stats at season start
statsAccumulator.InitializeLeagueSeasonStats(leagueId, season: 2026);
// Or for a single team
statsAccumulator.InitializeTeamSeasonStats(teamId, season: 2026);
```

## Batting Stats

### Record At-Bats

```csharp
// Single
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.Single);

// Double
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.Double);

// Triple
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.Triple);

// Home Run (automatically records a run)
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.HomeRun);

// Walk (doesn't count as an at-bat)
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.Walk);

// Strikeout
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.Strikeout);

// Out
statsAccumulator.RecordAtBat(playerId, teamId, season: 2026, PlayResultType.Out);
```

### Record RBIs

```csharp
// Batter drove in 2 runs
statsAccumulator.RecordRBI(playerId, teamId, season: 2026, rbiCount: 2);
```

### Record Runs

```csharp
// Runner scored on another player's hit
statsAccumulator.RecordRun(playerId, teamId, season: 2026);
```

### Record Stolen Bases

```csharp
// Single stolen base
statsAccumulator.RecordStolenBase(playerId, teamId, season: 2026);

// Multiple stolen bases (rare, but possible)
statsAccumulator.RecordStolenBase(playerId, teamId, season: 2026, baseCount: 2);
```

## Pitching Stats

### Record Pitching Performance

```csharp
// Pitched 5.2 innings, 8 strikeouts, 2 walks, 1 earned run
statsAccumulator.RecordPitchingStats(
    pitcherId: pitcherId,
    teamId: teamId,
    season: 2026,
    inningsPitched: 5.666, // 5.2 innings
    strikeouts: 8,
    walks: 2,
    earnedRuns: 1
);
```

### Record Win/Loss Decision

```csharp
// Pitcher gets the win
statsAccumulator.RecordPitcherDecision(pitcherId, teamId, season: 2026, isWin: true);

// Pitcher gets the loss
statsAccumulator.RecordPitcherDecision(pitcherId, teamId, season: 2026, isWin: false);
```

### Record Individual Pitching Events

```csharp
// Record a strikeout
statsAccumulator.RecordPitcherStrikeout(pitcherId, teamId, season: 2026);

// Record a walk allowed
statsAccumulator.RecordPitcherWalk(pitcherId, teamId, season: 2026);
```

## Game-Level Updates

### Update After Game Completion

```csharp
// Update game stats and record pitcher decisions
statsAccumulator.UpdateGameStats(
    game,
    winningPitcherId: homeTeamClosingPitcher.Id,
    losingPitcherId: awayTeamClosingPitcher.Id
);
```

## Querying Stats

### Get Player Season Stats

```csharp
var stats = statsAccumulator.GetPlayerSeasonStats(playerId, season: 2026);
if (stats != null)
{
    Console.WriteLine($"AVG: {stats.BattingAverage:F3}");
    Console.WriteLine($"HR: {stats.HomeRuns}");
    Console.WriteLine($"RBI: {stats.RunsBattedIn}");
    Console.WriteLine($"K: {stats.Strikeouts}");
}
```

### Get Player Career Stats

```csharp
var careerStats = statsAccumulator.GetPlayerCareerStats(playerId);
foreach (var seasonStat in careerStats)
{
    Console.WriteLine($"Season {seasonStat.Season}: AVG {seasonStat.BattingAverage:F3}");
}
```

### Get League Leaders

```csharp
// Batting leaders
var hrLeaders = statsAccumulator.GetLeagueLeaders(leagueId, season: 2026, "HR", limit: 10);
var avgLeaders = statsAccumulator.GetLeagueLeaders(leagueId, season: 2026, "AVG", limit: 10);
var rbiLeaders = statsAccumulator.GetLeagueLeaders(leagueId, season: 2026, "RBI", limit: 10);

// Pitching leaders
var strikeoutLeaders = statsAccumulator.GetLeagueLeaders(leagueId, season: 2026, "K", limit: 10);
var eraLeaders = statsAccumulator.GetLeagueLeaders(leagueId, season: 2026, "ERA", limit: 10);
var winLeaders = statsAccumulator.GetLeagueLeaders(leagueId, season: 2026, "W", limit: 10);
```

## Supported Stat Categories

### Batting Categories
- `HR` - Home Runs
- `RBI` - Runs Batted In
- `AVG` - Batting Average
- `R` - Runs
- `H` - Hits
- `SB` - Stolen Bases

### Pitching Categories
- `K` - Strikeouts
- `ERA` - Earned Run Average
- `W` - Wins
- `WHIP` - Walks + Hits per Innings Pitched

## Computed Stats (Automatic)

The `SeasonStats` model automatically computes these from raw stats:

- `BattingAverage` = Hits / At-Bats
- `OnBasePercentage` = (Hits + Walks) / (At-Bats + Walks)
- `SluggingPercentage` = Total Bases / At-Bats
- `ERA` = (9.0 × Earned Runs) / Innings Pitched
- `WinPercentage` = Wins / (Wins + Losses)

## Integration with Game Simulation

Typically, you'd use StatsAccumulator like this in your game simulator:

```csharp
public void SimulatePlay(Play play)
{
    var batter = repository.GetPlayer(play.BatterId);
    var pitcher = repository.GetPlayer(play.PitcherId);
    var season = 2026;

    // Determine play result
    var result = GeneratePlayResult(batter, pitcher);

    // Update stats
    statsAccumulator.RecordAtBat(batter.Id, batter.TeamId, season, result.Type, result.RunsScored);
    
    if (result.RBIs > 0)
        statsAccumulator.RecordRBI(batter.Id, batter.TeamId, season, result.RBIs);

    // Pitcher stats
    statsAccumulator.RecordPitcherStrikeout(pitcher.Id, pitcher.TeamId, season);
}
```

## Notes

- All timestamps are automatically set to UTC
- Stats are immediately saved to the database after each update
- The `UpdatedAt` field is automatically set on all operations
- Runs are automatically credited for home runs
- Walks do not count as at-bats (per baseball rules)
