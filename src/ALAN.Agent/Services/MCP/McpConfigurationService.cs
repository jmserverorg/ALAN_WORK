using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ALAN.Agent.Services.MCP;

/// <summary>
/// Service to configure MCP (Model Context Protocol) tools for the AI Agent.
/// Reads configuration from YAML and sets up MCP server connections.
/// </summary>
public class McpConfigurationService
{
    private readonly ILogger<McpConfigurationService> _logger;

    public McpConfigurationService(ILogger<McpConfigurationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load MCP configuration from YAML file and configure the agent with MCP tools.
    /// </summary>
    public void ConfigureMcpTools(AIAgent agent, string configPath)
    {
        if (!File.Exists(configPath))
        {
            _logger.LogWarning("MCP configuration file not found at {ConfigPath}", configPath);
            return;
        }

        try
        {
            _logger.LogInformation("Loading MCP configuration from {ConfigPath}", configPath);
            
            var yamlContent = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            var config = deserializer.Deserialize<McpConfig>(yamlContent);

            if (config?.Mcp?.Servers == null)
            {
                _logger.LogWarning("No MCP servers configured in {ConfigPath}", configPath);
                return;
            }

            foreach (var server in config.Mcp.Servers)
            {
                _logger.LogInformation("Configuring MCP server: {ServerName}", server.Key);
                _logger.LogInformation("  Command: {Command}", server.Value.Command);
                _logger.LogInformation("  Args: {Args}", string.Join(" ", server.Value.Args ?? new List<string>()));
                
                // Note: The actual MCP tool registration depends on the Agent Framework's MCP support
                // which may be in development. For now, we log the configuration.
                // When the Agent Framework MCP support is available, this is where we would:
                // 1. Create MCP client connection to the server
                // 2. Discover available tools from the MCP server
                // 3. Register those tools with the AI agent
                
                // Example (pseudo-code for when MCP support is available):
                // var mcpClient = new McpClient(server.Value.Command, server.Value.Args, server.Value.Env);
                // var tools = await mcpClient.ListToolsAsync();
                // foreach (var tool in tools)
                // {
                //     agent.AddTool(tool);
                // }
            }
            
            _logger.LogInformation("MCP configuration loaded successfully with {ServerCount} servers", 
                config.Mcp.Servers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MCP configuration from {ConfigPath}", configPath);
        }
    }
}

public class McpConfig
{
    public McpSection? Mcp { get; set; }
}

public class McpSection
{
    public Dictionary<string, McpServerConfig>? Servers { get; set; }
}

public class McpServerConfig
{
    public string Command { get; set; } = string.Empty;
    public List<string>? Args { get; set; }
    public Dictionary<string, string>? Env { get; set; }
}
