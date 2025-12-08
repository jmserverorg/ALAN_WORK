using Microsoft.Extensions.Logging;

namespace ALAN.Agent.Services.MCP;

/// <summary>
/// Manages multiple MCP server clients and provides unified access.
/// </summary>
public class MCPClientManager
{
    private readonly Dictionary<string, IMCPServerClient> _clients = new();
    private readonly ILogger<MCPClientManager> _logger;

    public MCPClientManager(ILogger<MCPClientManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register an MCP client
    /// </summary>
    public void RegisterClient(IMCPServerClient client)
    {
        _clients[client.ServerName] = client;
        _logger.LogInformation("Registered MCP client: {ServerName}", client.ServerName);
    }

    /// <summary>
    /// Get a registered MCP client by name
    /// </summary>
    public IMCPServerClient? GetClient(string serverName)
    {
        _clients.TryGetValue(serverName, out var client);
        return client;
    }

    /// <summary>
    /// Get all registered MCP clients
    /// </summary>
    public List<IMCPServerClient> GetAllClients()
    {
        return _clients.Values.ToList();
    }

    /// <summary>
    /// Connect to all registered MCP servers
    /// </summary>
    public async Task ConnectAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to all MCP servers");
        
        foreach (var client in _clients.Values)
        {
            try
            {
                await client.ConnectAsync(cancellationToken);
                _logger.LogInformation("Connected to {ServerName}", client.ServerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {ServerName}", client.ServerName);
            }
        }
    }

    /// <summary>
    /// List all available tools across all MCP servers
    /// </summary>
    public async Task<Dictionary<string, List<MCPTool>>> ListAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var allTools = new Dictionary<string, List<MCPTool>>();

        foreach (var client in _clients.Values)
        {
            if (client.IsConnected)
            {
                try
                {
                    var tools = await client.ListToolsAsync(cancellationToken);
                    allTools[client.ServerName] = tools;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to list tools for {ServerName}", client.ServerName);
                }
            }
        }

        return allTools;
    }

    /// <summary>
    /// Invoke a tool on a specific MCP server
    /// </summary>
    public async Task<MCPResponse> InvokeToolAsync(
        string serverName,
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient(serverName);
        if (client == null)
        {
            return new MCPResponse
            {
                Success = false,
                Error = $"MCP server '{serverName}' not found"
            };
        }

        if (!client.IsConnected)
        {
            return new MCPResponse
            {
                Success = false,
                Error = $"MCP server '{serverName}' not connected"
            };
        }

        return await client.InvokeToolAsync(toolName, parameters, cancellationToken);
    }

    /// <summary>
    /// Disconnect from all MCP servers
    /// </summary>
    public async Task DisconnectAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting from all MCP servers");
        
        foreach (var client in _clients.Values)
        {
            try
            {
                await client.DisconnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect from {ServerName}", client.ServerName);
            }
        }
    }

    /// <summary>
    /// Get connection status for all servers
    /// </summary>
    public Dictionary<string, bool> GetConnectionStatus()
    {
        return _clients.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.IsConnected
        );
    }
}
