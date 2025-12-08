# MCP Integration

ALAN integrates with Model Context Protocol (MCP) servers to access external tools and services.

## Available MCP Servers

### GitHub MCP Server
Provides tools for interacting with GitHub repositories:
- **list_repositories** - List repositories for an organization or user
- **get_file_contents** - Get contents of a file from a repository
- **list_commits** - List commits in a repository
- **create_pull_request** - Create a pull request with code changes
- **search_code** - Search for code across repositories

### Microsoft Learn MCP Server
Provides access to Microsoft Learn documentation and learning resources:
- **search_docs** - Search Microsoft Learn documentation
- **get_article** - Get a specific article or documentation page
- **list_learning_paths** - List available learning paths for a topic
- **get_code_samples** - Get code samples for a specific technology

## Usage

### Connecting to MCP Servers

MCP clients are automatically registered and can be accessed through the `MCPClientManager`:

```csharp
// Get the MCP client manager
var mcpManager = serviceProvider.GetRequiredService<MCPClientManager>();

// Connect to all servers
await mcpManager.ConnectAllAsync();

// Check connection status
var status = mcpManager.GetConnectionStatus();
// Returns: { "GitHub": true, "MicrosoftLearn": true }
```

### Listing Available Tools

```csharp
// List all tools from all servers
var allTools = await mcpManager.ListAllToolsAsync();

foreach (var (serverName, tools) in allTools)
{
    Console.WriteLine($"Server: {serverName}");
    foreach (var tool in tools)
    {
        Console.WriteLine($"  - {tool.Name}: {tool.Description}");
    }
}
```

### Invoking Tools

```csharp
// Invoke a GitHub tool
var response = await mcpManager.InvokeToolAsync(
    "GitHub",
    "list_repositories",
    new Dictionary<string, object>
    {
        ["owner"] = "microsoft"
    }
);

if (response.Success)
{
    Console.WriteLine($"Result: {response.Content}");
    var repos = response.Data["repositories"];
}
```

### Using Specific Clients

```csharp
// Get a specific MCP client
var githubClient = mcpManager.GetClient("GitHub");

if (githubClient != null && githubClient.IsConnected)
{
    var tools = await githubClient.ListToolsAsync();
    
    var response = await githubClient.InvokeToolAsync(
        "get_file_contents",
        new Dictionary<string, object>
        {
            ["owner"] = "microsoft",
            ["repo"] = "vscode",
            ["path"] = "README.md"
        }
    );
}
```

## Adding New MCP Servers

To add a new MCP server:

1. Create a class implementing `IMCPServerClient`:

```csharp
public class MyMCPClient : IMCPServerClient
{
    public string ServerName => "MyServer";
    
    public bool IsConnected { get; private set; }
    
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        // Connection logic
        IsConnected = true;
        return true;
    }
    
    public async Task<List<MCPTool>> ListToolsAsync(CancellationToken cancellationToken)
    {
        // Return list of available tools
        return new List<MCPTool>
        {
            new MCPTool
            {
                Name = "my_tool",
                Description = "Description of tool",
                Parameters = new Dictionary<string, MCPParameter>
                {
                    ["param1"] = new MCPParameter
                    {
                        Name = "param1",
                        Type = "string",
                        Required = true
                    }
                }
            }
        };
    }
    
    public async Task<MCPResponse> InvokeToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        // Tool invocation logic
        return new MCPResponse
        {
            Success = true,
            Content = "Result"
        };
    }
    
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = false;
    }
}
```

2. Register the client in `Program.cs`:

```csharp
builder.Services.AddSingleton<IMCPServerClient, MyMCPClient>();
```

The client will be automatically registered with the `MCPClientManager`.

## MCP Response Format

All tool invocations return an `MCPResponse`:

```csharp
public class MCPResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public string? Error { get; set; }
}
```

## Error Handling

MCP tool invocations handle errors gracefully:

```csharp
var response = await mcpManager.InvokeToolAsync(
    "GitHub",
    "invalid_tool",
    new Dictionary<string, object>()
);

if (!response.Success)
{
    Console.WriteLine($"Error: {response.Error}");
}
```

## Production Deployment

### Connecting to Real MCP Servers

In production, replace the simulated clients with actual MCP server connections:

1. Install the MCP SDK package
2. Configure server endpoints and authentication
3. Update the client implementation to use the SDK

Example:
```csharp
public class GitHubMCPClient : IMCPServerClient
{
    private readonly MCPClient _mcpClient;
    
    public GitHubMCPClient(IConfiguration config)
    {
        _mcpClient = new MCPClient(new MCPClientOptions
        {
            ServerUrl = config["MCP:GitHub:Url"],
            ApiKey = config["MCP:GitHub:ApiKey"]
        });
    }
    
    // Implement interface methods using _mcpClient
}
```

### Configuration

Add MCP server configuration to `appsettings.json`:

```json
{
  "MCP": {
    "GitHub": {
      "Url": "https://github-mcp.example.com",
      "ApiKey": "your-api-key"
    },
    "MicrosoftLearn": {
      "Url": "https://learn-mcp.microsoft.com",
      "ApiKey": "your-api-key"
    }
  }
}
```

## Integration with Agent

The agent can use MCP tools through the `MCPClientManager`:

1. Access GitHub repositories for self-improvement
2. Search Microsoft Learn for best practices
3. Retrieve code samples for learning
4. Create pull requests for code changes

The MCP integration enables the agent to:
- Read and analyze its own source code
- Propose improvements based on learned patterns
- Access external knowledge sources
- Interact with development tools and services

## Security Considerations

1. **Authentication** - Secure MCP server credentials
2. **Authorization** - Validate permissions before tool invocation
3. **Rate Limiting** - Respect API rate limits
4. **Input Validation** - Validate parameters before sending to MCP servers
5. **Audit Logging** - Log all MCP tool invocations for security audits
