namespace ALAN.Agent.Services.MCP;

/// <summary>
/// Interface for MCP (Model Context Protocol) server clients.
/// Allows the agent to interact with external tools and services.
/// </summary>
public interface IMCPServerClient
{
    /// <summary>
    /// Name of the MCP server (e.g., "GitHub", "MicrosoftLearn")
    /// </summary>
    string ServerName { get; }

    /// <summary>
    /// Connect to the MCP server
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if connected to the server
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// List available tools/functions from the MCP server
    /// </summary>
    Task<List<MCPTool>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoke a tool on the MCP server
    /// </summary>
    Task<MCPResponse> InvokeToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from the MCP server
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a tool/function available on an MCP server
/// </summary>
public class MCPTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, MCPParameter> Parameters { get; set; } = new();
    public string Category { get; set; } = "General";
}

/// <summary>
/// Represents a parameter for an MCP tool
/// </summary>
public class MCPParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
}

/// <summary>
/// Response from an MCP tool invocation
/// </summary>
public class MCPResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? Error { get; set; }
}
