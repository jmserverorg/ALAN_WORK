# Human Steering API

The Human Steering API provides REST endpoints for human operators to control and guide the autonomous agent.

## Endpoints

### Update Agent Prompt

Update the agent's current directive/prompt.

```http
POST /api/prompt
Content-Type: application/json

{
  "prompt": "Your new directive for the agent"
}
```

**Response:**
```json
{
  "message": "Prompt update queued",
  "prompt": "Your new directive for the agent"
}
```

### Pause Agent

Pause the autonomous agent's execution loop.

```http
POST /api/pause
```

**Response:**
```json
{
  "message": "Agent pause queued"
}
```

### Resume Agent

Resume the autonomous agent after being paused.

```http
POST /api/resume
```

**Response:**
```json
{
  "message": "Agent resume queued"
}
```

### Trigger Batch Learning

Manually trigger the batch learning process immediately (instead of waiting for the scheduled interval).

```http
POST /api/batch-learning
```

**Response:**
```json
{
  "message": "Batch learning trigger queued"
}
```

### Submit Custom Input

Submit a custom human input command with full control over type and parameters.

```http
POST /api/input
Content-Type: application/json

{
  "type": "UpdatePrompt",
  "content": "Your input content",
  "parameters": {
    "key": "value"
  }
}
```

**Input Types:**
- `UpdatePrompt` - Update agent's directive
- `PauseAgent` - Pause execution
- `ResumeAgent` - Resume execution
- `TriggerBatchLearning` - Start batch learning
- `ApproveCodeChange` - Approve proposed code changes
- `RejectCodeChange` - Reject proposed code changes
- `AddGoal` - Add a new goal
- `RemoveGoal` - Remove an existing goal
- `QueryState` - Query current agent state
- `ResetMemory` - Reset memory (use with caution)

**Response:**
```json
{
  "inputId": "generated-id",
  "message": "Input received and queued for processing",
  "timestamp": "2024-12-08T21:30:00Z"
}
```

### Get Agent State

Retrieve the current state of the agent.

```http
GET /api/state
```

**Response:**
```json
{
  "id": "agent-id",
  "status": "Thinking",
  "currentGoal": "Agent's current goal",
  "currentPrompt": "Active directive",
  "lastUpdated": "2024-12-08T21:30:00Z",
  "recentThoughts": [...],
  "recentActions": [...]
}
```

## Usage Examples

### Using curl

```bash
# Update the agent's prompt
curl -X POST http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Focus on learning about machine learning algorithms"}'

# Pause the agent
curl -X POST http://localhost:5000/api/pause

# Resume the agent
curl -X POST http://localhost:5000/api/resume

# Trigger batch learning
curl -X POST http://localhost:5000/api/batch-learning

# Get agent state
curl http://localhost:5000/api/state
```

### Using JavaScript/Fetch

```javascript
// Update prompt
async function updatePrompt(newPrompt) {
  const response = await fetch('/api/prompt', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt: newPrompt })
  });
  return await response.json();
}

// Pause agent
async function pauseAgent() {
  const response = await fetch('/api/pause', { method: 'POST' });
  return await response.json();
}

// Get state
async function getState() {
  const response = await fetch('/api/state');
  return await response.json();
}
```

### Using C#

```csharp
using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5000");

// Update prompt
var promptRequest = new { prompt = "New directive" };
var response = await httpClient.PostAsJsonAsync("/api/prompt", promptRequest);

// Pause agent
await httpClient.PostAsync("/api/pause", null);

// Get state
var state = await httpClient.GetFromJsonAsync<AgentState>("/api/state");
```

## Safety Considerations

1. **Authentication** - In production, add authentication to prevent unauthorized control
2. **Rate Limiting** - Implement rate limiting to prevent command flooding
3. **Audit Logging** - All human inputs are logged with timestamps
4. **Validation** - Input content is validated before processing
5. **Async Processing** - Commands are queued and processed asynchronously to avoid blocking

## Integration with Web UI

The web interface can use these APIs to provide:
- A prompt input box for updating directives
- Pause/Resume buttons
- Manual batch learning trigger
- Real-time state display
- Command history view

## Command Queue Behavior

- Commands are processed in FIFO order
- The agent checks for pending commands at the start of each loop iteration
- Commands are processed before the agent's normal thinking cycle
- Failed commands are logged but don't stop the agent loop
- Multiple commands can be queued and will be processed sequentially

## Error Handling

If a command fails, the API returns:

```json
{
  "inputId": "id",
  "success": false,
  "message": "Error description"
}
```

The agent continues running even if individual commands fail, ensuring robustness.
