using ALAN.Shared.Models;
using ALAN.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ALAN.Web.Services;

public class AgentStateService : BackgroundService
{
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly ILogger<AgentStateService> _logger;
    private AgentState _state = new();
    
    public AgentStateService(
        IHubContext<AgentHub> hubContext,
        ILogger<AgentStateService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent State Service starting (relay mode - waiting for agent updates)...");
        
        // Initial state
        _state.CurrentGoal = "Waiting for autonomous agent to connect";
        _state.Status = AgentStatus.Idle;
        
        // Just keep the service alive to relay messages
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent state service");
            }
        }
        
        _logger.LogInformation("Agent State Service stopped");
    }

    public async Task UpdateStateAsync(AgentState state)
    {
        _state = state;
        _state.LastUpdated = DateTime.UtcNow;
        
        // Broadcast state update to all connected clients
        await _hubContext.Clients.All.SendAsync("ReceiveStateUpdate", _state);
        _logger.LogDebug("Broadcasted state update: {Status}", state.Status);
    }

    public async Task BroadcastThoughtAsync(AgentThought thought)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveThought", thought);
        _logger.LogDebug("Broadcasted thought: {Type}", thought.Type);
    }

    public async Task BroadcastActionAsync(AgentAction action)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveAction", action);
        _logger.LogDebug("Broadcasted action: {Name}", action.Name);
    }
    
    public AgentState GetCurrentState()
    {
        return _state;
    }
}
