using OrchestratorChat.Saturn.Models;
using System.Reflection;

namespace OrchestratorChat.Saturn.Tools;

/// <summary>
/// Interface for tool registry
/// </summary>
public interface IToolRegistry
{
    void Register(ITool tool);
    void Unregister(string toolName);
    ITool? GetTool(string name);
    List<ITool> GetTools();
    List<ToolInfo> GetToolInfos();
    bool IsRegistered(string toolName);
}

/// <summary>
/// Tool registry implementation
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();

    public void Register(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (string.IsNullOrWhiteSpace(tool.Name))
            throw new ArgumentException("Tool name cannot be empty", nameof(tool));

        _tools[tool.Name] = tool;
    }

    public void Unregister(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return;

        _tools.Remove(toolName);
    }

    public ITool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    public List<ITool> GetTools()
    {
        return _tools.Values.ToList();
    }

    public List<ToolInfo> GetToolInfos()
    {
        return _tools.Values.Select(tool => new ToolInfo
        {
            Name = tool.Name,
            Description = tool.Description,
            RequiresApproval = tool.RequiresApproval,
            Parameters = tool.Parameters.ToList()
        }).ToList();
    }

    public bool IsRegistered(string toolName)
    {
        return !string.IsNullOrWhiteSpace(toolName) && _tools.ContainsKey(toolName);
    }

    /// <summary>
    /// Auto-register tools from assembly
    /// </summary>
    public void RegisterFromAssembly(Assembly assembly)
    {
        var toolTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && 
                       !t.IsInterface && 
                       typeof(ITool).IsAssignableFrom(t))
            .ToList();

        foreach (var toolType in toolTypes)
        {
            try
            {
                if (Activator.CreateInstance(toolType) is ITool tool)
                {
                    Register(tool);
                }
            }
            catch (Exception)
            {
                // Log or handle registration failure if needed
            }
        }
    }

    /// <summary>
    /// Register default tools
    /// </summary>
    public void RegisterDefaults()
    {
        RegisterFromAssembly(Assembly.GetExecutingAssembly());
    }
}