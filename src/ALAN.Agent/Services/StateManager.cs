using ALAN.Shared.Models;
using ALAN.Shared.Services.Memory;
using System.Collections.Concurrent;

namespace ALAN.Agent.Services;

public class StateManager
{
    private readonly ConcurrentQueue<AgentThought> _thoughts = new();
    private readonly ConcurrentQueue<AgentAction> _actions = new();
    private readonly ConcurrentDictionary<string, AgentAction> _actionDict = new();
    private AgentState _currentState = new();
    private readonly object _lock = new();
    private readonly IShortTermMemoryService _shortTermMemory;
    private readonly ILongTermMemoryService _longTermMemory;
    
    public event EventHandler<AgentState>? StateChanged;

    public StateManager(IShortTermMemoryService shortTermMemory, ILongTermMemoryService longTermMemory)
    {
        _shortTermMemory = shortTermMemory;
        _longTermMemory = longTermMemory;
    }
    
    public void AddThought(AgentThought thought)
    {
        _thoughts.Enqueue(thought);
        
        // Keep only recent thoughts
        while (_thoughts.Count > 100)
        {
            _thoughts.TryDequeue(out _);
        }
        
        UpdateState();
        
        // Store thought in both short-term (for web access) and long-term memory
        _ = _shortTermMemory.SetAsync($"thought:{thought.Id}", thought, TimeSpan.FromHours(24));
        _ = StoreThoughtAsync(thought);
    }
    
    public void AddAction(AgentAction action)
    {
        _actions.Enqueue(action);
        _actionDict[action.Id] = action;
        
        // Keep only recent actions
        while (_actions.Count > 50)
        {
            if (_actions.TryDequeue(out var old))
            {
                _actionDict.TryRemove(old.Id, out _);
            }
        }
        
        UpdateState();
        
        // Store action in both short-term (for web access) and long-term memory
        _ = _shortTermMemory.SetAsync($"action:{action.Id}", action, TimeSpan.FromHours(24));
        _ = StoreActionAsync(action);
    }
    
    public void UpdateAction(AgentAction action)
    {
        _actionDict[action.Id] = action;
        UpdateState();
        
        // Update action in both short-term and long-term memory
        _ = _shortTermMemory.SetAsync($"action:{action.Id}", action, TimeSpan.FromHours(24));
        _ = StoreActionAsync(action);
    }
    
    public void UpdateStatus(AgentStatus status)
    {
        lock (_lock)
        {
            _currentState.Status = status;
            _currentState.LastUpdated = DateTime.UtcNow;
        }
        
        NotifyStateChanged();
        PersistState();
    }
    
    public void UpdateGoal(string goal)
    {
        lock (_lock)
        {
            _currentState.CurrentGoal = goal;
            _currentState.LastUpdated = DateTime.UtcNow;
        }
        
        NotifyStateChanged();
        PersistState();
    }
    
    public void UpdatePrompt(string prompt)
    {
        lock (_lock)
        {
            _currentState.CurrentPrompt = prompt;
            _currentState.LastUpdated = DateTime.UtcNow;
        }
        
        NotifyStateChanged();
        PersistState();
    }
    
    public AgentState GetCurrentState()
    {
        lock (_lock)
        {
            var state = new AgentState
            {
                Id = _currentState.Id,
                Status = _currentState.Status,
                CurrentGoal = _currentState.CurrentGoal,
                CurrentPrompt = _currentState.CurrentPrompt,
                LastUpdated = DateTime.UtcNow,
                RecentThoughts = _thoughts.TakeLast(20).ToList(),
                RecentActions = _actions.TakeLast(10).ToList()
            };
            
            return state;
        }
    }
    
    private void UpdateState()
    {
        lock (_lock)
        {
            _currentState.RecentThoughts = _thoughts.TakeLast(20).ToList();
            _currentState.RecentActions = _actions.TakeLast(10).ToList();
            _currentState.LastUpdated = DateTime.UtcNow;
        }
        
        NotifyStateChanged();
        PersistState();
    }
    
    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, GetCurrentState());
    }

    private void PersistState()
    {
        // Store current state in both short-term and long-term memory
        var state = GetCurrentState();
        _ = _shortTermMemory.SetAsync("agent:current-state", state, TimeSpan.FromHours(1));
        
        // Also store in long-term memory as a special memory entry for cross-process access
        _ = Task.Run(async () =>
        {
            try
            {
                var stateMemory = new MemoryEntry
                {
                    Id = $"agent-state-{DateTime.UtcNow:yyyyMMddHHmmssfff}".ToLowerInvariant(),
                    Type = MemoryType.Decision,
                    Content = System.Text.Json.JsonSerializer.Serialize(state),
                    Summary = $"Agent State - Status: {state.Status}, Goal: {state.CurrentGoal}",
                    Importance = 1.0,
                    Tags = new List<string> { "agent-state", "system" },
                    Timestamp = DateTime.UtcNow
                };
                await _longTermMemory.StoreMemoryAsync(stateMemory);
            }
            catch
            {
                // Silently fail
            }
        });
    }

    private async Task StoreThoughtAsync(AgentThought thought)
    {
        try
        {
            var memory = new MemoryEntry
            {
                Id = thought.Id,
                Type = thought.Type switch
                {
                    ThoughtType.Observation => MemoryType.Observation,
                    ThoughtType.Reasoning => MemoryType.Decision,
                    ThoughtType.Reflection => MemoryType.Reflection,
                    _ => MemoryType.Observation
                },
                Content = thought.Content,
                Summary = $"{thought.Type}: {thought.Content.Substring(0, Math.Min(50, thought.Content.Length))}...",
                Importance = 0.4,
                Tags = new List<string> { "thought", thought.Type.ToString().ToLower() },
                Timestamp = thought.Timestamp
            };

            await _longTermMemory.StoreMemoryAsync(memory);
        }
        catch (Exception)
        {
            // Silently fail - don't interrupt agent operation
        }
    }

    private async Task StoreActionAsync(AgentAction action)
    {
        try
        {
            var memory = new MemoryEntry
            {
                Id = action.Id,
                Type = action.Status == ActionStatus.Completed ? MemoryType.Success : MemoryType.Decision,
                Content = $"Action: {action.Name}\nDescription: {action.Description}\nInput: {action.Input}\nOutput: {action.Output}",
                Summary = $"{action.Name}: {action.Description}",
                Importance = 0.6,
                Tags = new List<string> { "action", action.Status.ToString().ToLower(), action.Name.ToLower() },
                Timestamp = action.Timestamp
            };

            await _longTermMemory.StoreMemoryAsync(memory);
        }
        catch (Exception)
        {
            // Silently fail - don't interrupt agent operation
        }
    }
}
