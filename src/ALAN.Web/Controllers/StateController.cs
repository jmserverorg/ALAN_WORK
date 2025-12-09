using ALAN.Shared.Models;
using ALAN.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ALAN.Web.Controllers;

[ApiController]
[Route("api")]
public class StateController : ControllerBase
{
    private readonly AgentStateService _stateService;
    
    public StateController(AgentStateService stateService)
    {
        _stateService = stateService;
    }
    
    [HttpGet("state")]
    public IActionResult GetState()
    {
        var state = _stateService.GetCurrentState();
        return Ok(state);
    }

    [HttpPost("state")]
    public async Task<IActionResult> UpdateState([FromBody] AgentState state)
    {
        await _stateService.UpdateStateAsync(state);
        return Ok();
    }

    [HttpPost("thought")]
    public async Task<IActionResult> AddThought([FromBody] AgentThought thought)
    {
        await _stateService.BroadcastThoughtAsync(thought);
        return Ok();
    }

    [HttpPost("action")]
    public async Task<IActionResult> AddAction([FromBody] AgentAction action)
    {
        await _stateService.BroadcastActionAsync(action);
        return Ok();
    }
}
