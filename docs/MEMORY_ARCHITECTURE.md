# Memory Architecture

ALAN includes a sophisticated memory system that enables self-improvement through learning and consolidation.

## Overview

The memory architecture consists of three main components:

1. **Short-term Memory** - Working memory for current context and tasks
2. **Long-term Memory** - Persistent storage for historical context, logs, and learnings
3. **Memory Consolidation** - Batch process that analyzes memories and extracts learnings

## Memory Types

### Short-term Memory (Working Memory)
- Stores temporary data with optional expiration
- Used for current session state and immediate context
- Implemented in-memory (can be replaced with Redis for production)
- Automatically expires old entries

### Long-term Memory
- Stores permanent memories of observations, decisions, actions, errors, and learnings
- Each memory has:
  - Type (Observation, Learning, CodeChange, Decision, Reflection, Error, Success)
  - Content and summary
  - Metadata tags
  - Importance score
  - Access tracking
- Supports searching and filtering
- Can be backed by Azure AI Search or Azure Cosmos DB

### Memory Entry Structure

```csharp
public class MemoryEntry
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public MemoryType Type { get; set; }
    public string Content { get; set; }
    public string Summary { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public int AccessCount { get; set; }
    public DateTime LastAccessed { get; set; }
    public double Importance { get; set; }  // 0.0 to 1.0
    public List<string> Tags { get; set; }
}
```

## Batch Learning Process

The batch learning process runs periodically to:
1. Extract learnings from recent memories
2. Consolidate similar memories into higher-level insights
3. Clean up outdated or low-importance memories
4. Store consolidated learnings for future reference

### Configuration

Batch learning is triggered when:
- A configurable number of iterations have passed (default: 100)
- OR a time threshold is exceeded (default: 4 hours)

To configure batch learning intervals, modify the service initialization in your code.

### Batch Learning Output

Consolidated learnings include:
- Topic summary
- Key insights extracted from multiple memories
- Confidence score
- References to source memories

## Usage Examples

### Storing a Memory

```csharp
var memory = new MemoryEntry
{
    Type = MemoryType.Learning,
    Content = "Discovered that async operations improve responsiveness",
    Summary = "Async programming benefit",
    Importance = 0.8,
    Tags = new List<string> { "async", "performance", "learning" }
};

await longTermMemory.StoreMemoryAsync(memory);
```

### Searching Memories

```csharp
// Search for memories containing specific keywords
var memories = await longTermMemory.SearchMemoriesAsync("async", maxResults: 10);

// Get memories by type
var learnings = await longTermMemory.GetMemoriesByTypeAsync(MemoryType.Learning);

// Get recent memories
var recent = await longTermMemory.GetRecentMemoriesAsync(count: 50);
```

### Using Short-term Memory

```csharp
// Store temporary data
await shortTermMemory.SetAsync("current_task", taskInfo, TimeSpan.FromHours(1));

// Retrieve data
var task = await shortTermMemory.GetAsync<TaskInfo>("current_task");

// Check existence
bool exists = await shortTermMemory.ExistsAsync("current_task");
```

## Production Deployment

### Azure AI Search Integration

For production use, replace `InMemoryLongTermMemoryService` with an Azure AI Search implementation:

1. Create an Azure AI Search resource
2. Configure search indexes for memory entries
3. Implement vector search for semantic similarity
4. Enable full-text search on content and summary fields

### Redis Cache Integration

For distributed short-term memory, replace `InMemoryShortTermMemoryService` with Redis:

1. Create an Azure Cache for Redis instance
2. Install the StackExchange.Redis NuGet package
3. Implement the `IShortTermMemoryService` interface using Redis commands
4. Configure connection strings and failover policies

## Architecture Benefits

1. **Scalability** - In-memory services can be swapped for Azure services without code changes
2. **Learning** - Batch process extracts patterns and insights automatically
3. **Maintenance** - Old memories are automatically cleaned up
4. **Searchability** - Memories can be queried by content, type, or tags
5. **Importance Weighting** - Critical memories are retained longer

## Integration with Agent Loop

The autonomous agent automatically:
- Stores observations before each thinking cycle
- Records reasoning and decisions in long-term memory
- Logs successful actions and errors
- Pauses for batch learning at configured intervals
- Uses memory context to inform future decisions

## Future Enhancements

- Semantic embeddings for similarity search
- Memory importance decay over time
- Cross-agent memory sharing
- Knowledge graph construction
- Automated memory categorization
