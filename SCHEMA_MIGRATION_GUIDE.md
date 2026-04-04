# Database Schema Migration Guide: String IDs → Integer PKs with GUIDs

This document outlines the complete strategy for migrating the baseball-cli database from string-based IDs (GUIDs) to a dual-ID pattern:
- **Integer auto-increment primary key** (uint) for performance and foreign keys
- **Separate GUID column** for external API/distributed system references

## Why This Matters

**Current Schema Problems:**
- String PKs use more disk space (36 bytes for GUID vs 4 bytes for uint)
- Foreign key relationships on strings are slower than integers
- Harder to generate meaningful IDs for APIs/external systems
- String keys can be accidentally modified

**Benefits of New Schema:**
- 90% smaller indexes and foreign keys
- Faster join operations and lookups
- Clean separation: internal database IDs vs external GUIDs
- Support for composite keys and complex relationships
- Better performance for high-transaction scenarios

## Migration Strategy

The migration is a breaking change affecting all models, repositories, and services. Here's the complete approach:

### Phase 1: Update Domain Models

Update `/Models/DomainModels.cs`:

```csharp
public class League
{
    // Auto-increment PK (internal use)
    public uint Id { get; set; }
    
    // External GUID identifier (APIs, external refs)
    public string Guid { get; set; } = null!;
    
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    // ... navigation properties
}

// Similar for: Team, Player, Game, Play, SeasonStats, TeamStats
```

**Key Changes for All Entities:**
1. Change `string Id` → `uint Id` (auto-increment)
2. Add `string Guid` property (unique GUID)
3. Update all foreign keys from `string` to `uint`
4. Add unique index on `Guid` column

### Phase 2: Update EF Core Configuration

In `OnModelCreating`:

```csharp
modelBuilder.Entity<League>()
    .HasKey(l => l.Id);

// Mark as auto-increment
modelBuilder.Entity<League>()
    .Property(l => l.Id)
    .ValueGeneratedOnAdd();

// Unique GUID index
modelBuilder.Entity<League>()
    .HasIndex(l => l.Guid)
    .IsUnique();

// Other indexes/relationships...
```

Do this for all 7 entities: League, Team, Player, Game, Play, SeasonStats, TeamStats

### Phase 3: Update Repository Methods

All repository method signatures need updating:

**Before:**
```csharp
public Team? GetTeam(string teamId)
public List<Player> GetPlayersByTeam(string teamId)
public List<Game> GetGamesByLeague(string leagueId)
```

**After:**
```csharp
public Team? GetTeam(uint teamId)
public List<Player> GetPlayersByTeam(uint teamId)
public List<Game> GetGamesByLeague(uint leagueId)
```

### Phase 4: Update Services

Update all services that reference IDs:

**Files to update:**
- `Services/StatsAccumulator.cs` - All `string playerId, teamId, etc` → `uint`
- `Services/GameSimulator.cs` - Foreign key references
- `Services/SeasonSimulator.cs` - Season/team/player ID handling
- `Services/GameBalance.cs` - If using ID filters
- `Commands/*.cs` - Any ID parsing/display logic

### Phase 5: Database Migration

Since this is a schema-breaking change with no existing production data, use:

```csharp
_dbContext.Database.EnsureDeleted();  // Clear old schema
_dbContext.Database.EnsureCreated();  // Create new schema
```

For production scenarios with existing data, create an EF Core migration:

```bash
dotnet ef migrations add ConvertStringIDsToUint
dotnet ef database update
```

### Phase 6: Configuration Loading

Update `LoadSeasonConfiguration` in BaseballDbContext:

```csharp
public void LoadSeasonConfiguration(SeasonConfiguration config)
{
    var league = new League
    {
        Guid = Guid.NewGuid().ToString(),  // Generate GUID
        Name = config.League.Name,
        CreatedAt = DateTime.UtcNow
        // Id auto-generated!
    };
    _context.Leagues.Add(league);
    _context.SaveChanges();  // Now league.Id is populated
    
    foreach (var teamConfig in config.Teams)
    {
        var team = new Team
        {
            Guid = Guid.NewGuid().ToString(),
            LeagueId = league.Id,  // Use uint ID now!
            // ...
        };
        // ...
    }
}
```

## Implementation Checklist

- [ ] Update all 7 entity models (League, Team, Player, Game, Play, SeasonStats, TeamStats)
- [ ] Update `OnModelCreating` with new key configurations
- [ ] Update BaseballRepository method signatures (10+ methods)
- [ ] Update StatsAccumulator service (12+ methods)
- [ ] Update GameSimulator  service
- [ ] Update SeasonSimulator service
- [ ] Update DrillDownInterface command
- [ ] Update SimulationController service
- [ ] Update LoadSeasonConfiguration method
- [ ] Update any ID parsing in CLI commands
- [ ] Drop and recreate database for testing
- [ ] Test full workflow: new season → load → query standings → record stats
- [ ] Commit all changes together

## Total Changes Required

**Estimated scope:**
- ~7 model classes: +2 properties each, -1 property modified
- ~40+ method signatures: string → uint
- ~100+ method calls: parameter type conversions
- ~1 configuration section: schema recreation

**Effort:** ~2-3 hours for complete implementation

## Benefits After Migration

1. **Performance**: Integer joins 3-5x faster than string joins
2. **Storage**: Indexes ~90% smaller
3. **API Design**: GUIDs perfect for REST APIs, integers for internal queries
4. **Scalability**: Easier to add sharding/partitioning with integer keys
5. **Consistency**: Clear separation of concerns (DB vs external)

##Testing Strategy

After migration:

```csharp
// Test database creation
var dbContext = new BaseballDbContext("test.db");
dbContext.Database.EnsureCreated();

// Test loading season
var config = ConfigLoader.LoadConfig("test-season");
dbContext.LoadSeasonConfiguration(config);

// Verify IDs are generated
var league = dbContext.Leagues.First();
Assert.True(league.Id > 0);
Assert.NotEmpty(league.Guid);

// Test stats recording
var accumulator = new StatsAccumulator(repository);
var player = dbContext.Players.First();
accumulator.RecordAtBat(player.Id, player.TeamId, 2026, PlayResultType.Single);

var stats = repository.GetSeasonStats(player.Id, 2026);
Assert.NotNull(stats);
```

## Rollback Plan

If issues arise:
1. `git revert` to previous commit
2. `Database.EnsureDeleted()`
3. `Database.EnsureCreated()` with old schema
4. Reload config data

## Notes

- All existing databases will need to be recreated
- Backward compatibility not maintained (breaking change)
- Consider implementing queryable-by-GUID methods for APIs
- Document both ID types in API documentation
- Add example: "Get player by GUID" method for external references

```csharp
public Player? GetPlayerByGuid(string guid)
{
    return _context.Players
        .FirstOrDefault(p => p.Guid == guid);
}
```

---

**When Ready to Implement:**
1. Create a new branch: `git checkout -b feature/uint-pk-migration`
2. Follow the checklist above
3. Test thoroughly before merging
4. Commit message: "Migrate to uint PKs with GUID identifiers - breaking schema change"
