using ALAN.Shared.Models;
using ALAN.Shared.Services.Memory;
using ALAN.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ALAN.Web.Services;

public class AgentStateService : BackgroundService
{
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly ILogger<AgentStateService> _logger;
    private readonly IShortTermMemoryService _shortTermMemory;
    private readonly ILongTermMemoryService _longTermMemory;
    private AgentState _state = new();
    private readonly HashSet<string> _seenThoughtIds = new();
    private readonly HashSet<string> _seenActionIds = new();
    
    public AgentStateService(
        IHubContext<AgentHub> hubContext,
        ILogger<AgentStateService> logger,
        IShortTermMemoryService shortTermMemory,
        ILongTermMemoryService longTermMemory)
    {
        _hubContext = hubContext;
        _logger = logger;
        _shortTermMemory = shortTermMemory;
        _longTermMemory = longTermMemory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent State Service starting (pull mode - reading from shared memory)...");
        
        // Initial state
        _state.CurrentGoal = "Waiting for autonomous agent to start";
        _state.Status = AgentStatus.Idle;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PullStateFromMemoryAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent state service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Agent State Service stopped");
    }

    private async Task PullStateFromMemoryAsync(CancellationToken cancellationToken)
    {
        // Pull current state from long-term memory (shared across processes)
        var stateMemories = await _longTermMemory.GetRecentMemoriesAsync(1, cancellationToken);
        var stateMemory = stateMemories.FirstOrDefault(m => m.Tags.Contains("agent-state"));
        
        AgentState? currentState = null;
        if (stateMemory != null)
        {
            try
            {
                currentState = System.Text.Json.JsonSerializer.Deserialize<AgentState>(stateMemory.Content);
            }
            catch
            {
                // Failed to deserialize, will use default state
            }
        }
        
        // Pull recent thoughts and actions from long-term memory
        var recentThoughts = await _longTermMemory.GetRecentMemoriesAsync(20, cancellationToken);
        var recentActions = await _longTermMemory.GetRecentMemoriesAsync(15, cancellationToken);
        
        // Convert memories back to thoughts and actions
        var thoughts = recentThoughts
            .Where(m => m.Tags.Contains("thought"))
            .Select(m => new AgentThought
            {
                Id = m.Id,
                Type = m.Type switch
                {
                    MemoryType.Observation => ThoughtType.Observation,
                    MemoryType.Decision => ThoughtType.Reasoning,
                    MemoryType.Reflection => ThoughtType.Reflection,
                    _ => ThoughtType.Observation
                },
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .OrderBy(t => t.Timestamp)
            .ToList();
            
        var actions = recentActions
            .Where(m => m.Tags.Contains("action"))
            .Select(m => new AgentAction
            {
                Id = m.Id,
                Name = m.Tags.FirstOrDefault(t => t != "action" && !t.Contains("completed") && !t.Contains("pending")) ?? "unknown",
                Description = m.Summary,
                Status = m.Tags.Contains("completed") ? ActionStatus.Completed : ActionStatus.Pending,
                Timestamp = m.Timestamp
            })
            .OrderBy(a => a.Timestamp)
            .ToList();
        
        if (currentState != null)
        {
            var stateChanged = _state.Status != currentState.Status || 
                              _state.CurrentGoal != currentState.CurrentGoal ||
                              _state.CurrentPrompt != currentState.CurrentPrompt;

            _state = currentState;
            
            if (stateChanged)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveStateUpdate", _state, cancellationToken);
                _logger.LogDebug("Broadcasted state update: {Status}", _state.Status);
            }
        }
        else
        {
            // No state in memory yet, create default
            _state = new AgentState
            {
                CurrentGoal = "Waiting for autonomous agent to start",
                Status = AgentStatus.Idle
            };
        }

        // Populate state with recent thoughts and actions for API endpoint
        _state.RecentThoughts = thoughts.TakeLast(20).ToList();
        _state.RecentActions = actions.TakeLast(15).ToList();

        // Broadcast new thoughts
        foreach (var thought in thoughts.Where(t => !_seenThoughtIds.Contains(t.Id)))
        {
            await _hubContext.Clients.All.SendAsync("ReceiveThought", thought, cancellationToken);
            _seenThoughtIds.Add(thought.Id);
            _logger.LogDebug("Broadcasted thought: {Type}", thought.Type);
        }

        // Broadcast new actions  
        foreach (var action in actions.Where(a => !_seenActionIds.Contains(a.Id)))
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAction", action, cancellationToken);
            _seenActionIds.Add(action.Id);
            _logger.LogDebug("Broadcasted action: {Name}", action.Name);
        }

        // Cleanup old IDs to prevent memory bloat
        if (_seenThoughtIds.Count > 1000)
        {
            var toRemove = _seenThoughtIds.Take(_seenThoughtIds.Count - 500).ToList();
            foreach (var id in toRemove)
                _seenThoughtIds.Remove(id);
        }

        if (_seenActionIds.Count > 500)
        {
            var toRemove = _seenActionIds.Take(_seenActionIds.Count - 250).ToList();
            foreach (var id in toRemove)
                _seenActionIds.Remove(id);
        }
    }
    
    public AgentState GetCurrentState()
    {
        return _state;
    }
}
