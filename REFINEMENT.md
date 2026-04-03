# Baseball CLI - Refinement Guide

This guide documents balance tuning, edge case handling, and optimization opportunities in baseball-cli.

## Game Balance Analysis

### Detecting Imbalances

Use `GameBalance.AnalyzeSeasonBalance()` to detect:
- Extreme win percentage variance (flag if > 50% difference)
- Unbalanced league (win % variance > 5%)
- Outlier teams with unrealistic records

### Tuning Probability Multipliers

Multipliers control how much different batter-pitcher matchups favor one side:
- **Multiplier < 1.0**: Favors pitcher (fewer hits)
- **Multiplier = 1.0**: Neutral
- **Multiplier > 1.0**: Favors batter (more hits)

**Suggested ranges by matchup:**
- Right-handed batter vs Right-handed pitcher: 0.95-1.05
- Right-handed batter vs Left-handed pitcher: 1.05-1.15 (advantage to batter)
- Left-handed batter vs Right-handed pitcher: 1.05-1.15 (advantage to batter)
- Left-handed batter vs Left-handed pitcher: 0.90-1.00

### Expected Statistics

**MLB Averages (realistic targets):**
- Batting Average: 0.250-0.300
- Home Runs per 500 AB: 25-35 (1 per 15-20 AB)
- Strikeouts: 20-25% of at-bats
- Walks: 8-10% of at-bats
- Average Runs per Game: 4-5 per team

**How to detect issues:**
```csharp
// Check if scoring is realistic
var (isValid, issues) = GameBalance.ValidateScoringRealism(games);
if (!isValid)
{
    foreach (var issue in issues)
        Console.WriteLine($"Balance Issue: {issue}");
}
```

### Home Field Advantage

Validation checks that home teams win ~54% of games (realistic MLB figure).

**Acceptable range:** 50-58% home win percentage

If outside range:
- **Too high (>58%):** Home field advantage is too strong, reduce pitcher/batter adjustment algorithms
- **Too low (<50%):** Reverse the advantage, or check if away teams have better players

## Edge Case Handling

### Score Validation

Games should produce scores in realistic range (0-20 runs each team):

```csharp
if (!EdgeCaseHandling.IsValidGameScore(homeScore, awayScore))
{
    Console.WriteLine("Warning: Unrealistic game score generated");
}
```

### Player Statistics

**Empty stats:** If a player has 0 at-bats, use league average (.250) instead of .000

```csharp
var average = EdgeCaseHandling.GetSafePlayerAverage(player);
```

**ERA edge case:** Pitchers with <1 inning pitched should show "—" instead of extreme ERA

### Division by Zero

Statistics calculations must handle zero denominators:

```csharp
// Bad:  var ba = hits / atBats;        // Throws if atBats=0
// Good: var ba = SafeDivide(hits, atBats, 0);
```

### Game Validation

Before simulating a game, validate:
- Both teams exist and are different
- Each team has 9+ players
- Players have valid stats (not negative)
- Game date is within season

```csharp
if (!EdgeCaseHandling.IsValidGame(game))
{
    Console.WriteLine("Cannot simulate invalid game");
    return;
}
```

## Optimization Opportunities

### 1. Probability Table Caching
Current: Probability tables parsed from config each time
Better: Cache parsed tables in memory, reload only when config changes

### 2. Schedule Generation
Current: Generates all games upfront
Better: Lazy generation or on-demand for very long seasons

### 3. Event Distribution
Current: Each event independently random
Better: Track probabilities to ensure event distribution matches expectations

### 4. Database Queries
Current: Full loads for stats aggregation
Better: Use SQL aggregations (COUNT, SUM, AVG) for large datasets

### 5. Simulation Performance
Target: Full 162-game season < 5 seconds
Current: ~10-30 seconds depending on verbosity

Optimization ideas:
- Batch database inserts
- Reduce real-time viewer overhead
- Parallel game simulation (if stateless)

## Known Issues & Workarounds

### Issue 1: Pitcher Batting Average
**Problem:** Pitchers have default .200 BA, but don't actually bat in many games
**Workaround:** Exclude pitchers from "leaders" lists, or cap pitcher at-bats
**Fix:** Track designated hitter separately, or implement DH rule option

### Issue 2: Simplistic Base Runner Tracking
**Problem:** BaseState only tracks occupied bases, not which runner on each
**Workaround:** Adequate for MVP, doesn't affect scoring much
**Fix:** Enhance BaseState to track individual runners and their stats

### Issue 3: Batting Order Auto-Generated
**Problem:** Simple sort by position, no consideration of player skill
**Workaround:** Manual order configuration in config file
**Fix:** Implement lineup optimization based on player stats

### Issue 4: No Injury Recovery
**Problem:** Injuries aren't tracked or recovered from
**Workaround:** Manually adjust player stats if needed
**Fix:** Implement injury system with recovery timeline

## Testing for Quality

### Smoke Tests (run after changes)
1. Create season and simulate 1 game → Should complete without errors
2. Check standings → Should show valid W-L records
3. Display stats → Should not show negative values or infinities
4. Player comparison → Should work for both batters and pitchers

### Balance Tests (run after probability changes)
1. Simulate 100 games → Check win % distribution
2. Analyze player stats → Verify against MLB baselines
3. Check home field advantage → Should be ~54%

### Regression Tests (before committing)
1. Ensure all 15 unit/integration tests pass
2. Run manual full-season simulation
3. Verify no infinite loops or crashes
4. Check memory usage stays reasonable

## Tuning Checklist

Before declaring the game "balanced":

- [ ] Average home team win % is 52-56% (0.54 ± 0.04)
- [ ] Average runs per game is 4-6 per team (realistic: 4-5)
- [ ] Batting average distribution matches MLB (median ~.260)
- [ ] No team wins >70% or loses >70% of games
- [ ] No extreme score outliers (>20 runs in single game)
- [ ] Strikeout rate 20-25% of at-bats
- [ ] Home run rate 1 per 15-20 at-bats
- [ ] Walk rate 8-10% of at-bats
- [ ] All tests pass
- [ ] Simulation runs without crashes
- [ ] Performance acceptable (<30 sec for full season)

## Probability Tuning Workflow

1. **Baseline:** Run season with default probabilities
2. **Analyze:** Check game stats using `GameBalance` utilities
3. **Identify:** Find what's off (too many HRs? Low strikeouts?)
4. **Adjust:** Modify probability multipliers in config
5. **Validate:** Re-run and compare results
6. **Repeat:** Until stats match targets

### Example Tuning Session

```
Initial run: Avg HR rate 1 per 12 AB (too high, expected 1 per 18)
Action: Lower all HR probability multipliers by 0.9x
Second run: Avg HR rate 1 per 18 AB (perfect)

Result: Season now realistic. Commit new config.
```

## Performance Tuning

### Profiling Targets
- Season simulation: Target < 5 seconds for 162 games
- Per-game overhead: < 50ms
- Database insert batch: 100+ games/sec

### When to Optimize
1. If full season takes > 30 seconds
2. If memory usage > 100MB
3. If real-time viewer causes lag

### Quick Wins
1. Disable real-time display (`--verbosity minimal`)
2. Batch database operations
3. Use in-memory database for testing

## Regression Prevention

Automated tests ensure quality:
- 7 unit tests cover core logic
- 8 integration tests cover workflows
- Run after every major change
- Add tests when bugs are found

## Documentation Updates

When tuning or fixing:
1. Document what was changed and why
2. Update this file with new findings
3. Update USAGE.md if user-facing behavior changed
4. Add test cases for new edge cases

---

**Last Updated:** 2024-04-03
**Next Review:** After 100 games simulated with new config
