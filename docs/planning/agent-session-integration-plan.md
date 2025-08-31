# Agent Creation, Session Wiring, and SignalR Integration Plan

This plan aligns the agent lifecycle across Web UI, persistence, and SignalR so that creating an agent in the UI translates into the correct runtime agent (Claude or Saturn) used during chat and orchestration.

## Context and Findings

- UI `AgentService` keeps agents in an in‑memory dictionary; it does not persist to the Data layer.
- `AgentHub` (SignalR) maintains its own static `_activeAgents` and calls `AgentFactory.CreateAgentAsync(AgentType.Claude, new AgentConfiguration())` whenever it needs an agent, ignoring the UI’s chosen type/config.
- Data project has entities/repos for agents and configurations (`AgentEntity`, `AgentConfigurationEntity`), but the UI/path doesn’t save agents there yet.
- Result: Users “create” agents in UI but chat often instantiates a new Claude agent; if Claude CLI isn’t installed/authenticated, message send fails.

Primary goal: Single source of truth for agent definitions + centralized runtime registry so both Web and SignalR use the same configuration and type.

## Goals

- Persist agent definitions (name, type, provider, model, settings) via EF repositories.
- Introduce a singleton `IAgentRegistry` for active agent instances shared across Web and Hubs.
- Update SignalR hubs to resolve agent by ID via persistence → build via `AgentFactory` with correct type/config → cache in `IAgentRegistry`.
- Keep a clean, testable separation between persistence (Data) and runtime (Agents/SignalR).

## Non‑Goals (for this pass)

- Overhauling UI/UX or adding advanced orchestration features.
- Rewriting Claude/Saturn adapters beyond what’s required to pass in configuration cleanly.

## Design

1) Persistence model usage (Data project)
- Use existing `AgentEntity` and `AgentConfigurationEntity` for durable storage.
- `IAgentRepository` adds/fetches/updates agent definitions.

2) Runtime registry (new)
- `IAgentRegistry` (singleton):
  - `Task<IAgent> GetOrCreateAsync(string agentId, Func<Task<(AgentType type, AgentConfiguration cfg)>> factory)`
  - `Task<IAgent?> FindAsync(string agentId)` / `Task RemoveAsync(string agentId)`
  - Holds `ConcurrentDictionary<string, IAgent>`; disposes on removal.

3) Agent resolution flow
- UI creates an agent → `AgentService` persists via `IAgentRepository` and returns the saved ID.
- Chat send:
  - `AgentHub.SendAgentMessage` calls `registry.GetOrCreateAsync(agentId, ...)`
  - Factory callback queries `IAgentRepository.GetWithConfigurationAsync(agentId)`; throws if not found.
  - Map repository config → `Core.Agents.AgentConfiguration` (including `CustomSettings` like `Provider`/`ApiKey`)
  - Call `AgentFactory.CreateAgentAsync(storedType, cfg)` → cache in registry.

4) Type selection rules
- Respect the stored `AgentType` (Claude|Saturn).
- Saturn: if `CustomSettings["Provider"]` is missing, default to `OpenRouter`; pass through `ApiKey` if present, else rely on `OPENROUTER_API_KEY` env var.
- Claude: if `claude` CLI is missing, fail fast with a clear message sent over SignalR (`ErrorResponse`).

5) Session wiring
- When creating a session, allow agent selection that references persisted agent IDs.
- Optionally pre‑warm session agents via `IAgentRegistry.GetOrCreateAsync` so the first message is snappy.

## Work Items

1) Implement `IAgentRegistry` (new file in `src/OrchestratorChat.Agents/` or `src/OrchestratorChat.Core/Agents/Runtime`)
- Singleton service; DI registration in Web `Program.cs` and server startup.
- Clean disposal of agents on removal/app shutdown.

2) Persist agents from UI
- Update `src/OrchestratorChat.Web/Services/AgentService.cs` to depend on `IAgentRepository` and write/read agents from DB instead of local dictionary.
- Expand `AgentInfo` ↔ entity mapping to include `Type`, `WorkingDirectory`, `CustomSettings` (e.g., provider, model, require approval).

3) Fix SignalR hub agent lookup
- `src/OrchestratorChat.SignalR/Hubs/AgentHub.cs` → replace static `_activeAgents` and the hardcoded Claude creation.
- New flow:
  - `var agent = await _registry.GetOrCreateAsync(request.AgentId, async () => { var stored = await _agentRepo.GetWithConfigurationAsync(request.AgentId); /* map */ return (stored.Type, cfg); });`
  - If not found → `Clients.Caller.ReceiveError(...)` with friendly guidance.

4) Align Chat UI with persisted agents
- The Chat page already sends `AgentId`; ensure the list shown comes from `AgentService` reading the repo (after item 2).
- Optional: Provide a basic “Agent Settings” dialog that reads stored config (read‑only for now) so it’s clear which provider/model will be used.

5) Configuration
- Add minimal mapping for Saturn provider defaults in appsettings (optional) or rely on env vars; document in USER_GUIDE.md and CLAUDE.md.

6) Logging and errors
- Ensure initialization errors (e.g., missing CLI or API key) surface as `ErrorResponse` over SignalR and via UI console logs.

7) Tests
- Web tests: verify `AgentService` persists and returns stored agents.
- SignalR integration tests: sending a message to a stored Saturn/Claude agent ID uses the right adapter and streams text.

## Implementation Notes

Example `IAgentRegistry` skeleton:
```csharp
public interface IAgentRegistry
{
    Task<IAgent?> FindAsync(string agentId);
    Task<IAgent> GetOrCreateAsync(string agentId, Func<Task<(AgentType type, AgentConfiguration cfg)>> factory);
    Task RemoveAsync(string agentId);
}

public class AgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new();

    public Task<IAgent?> FindAsync(string agentId) =>
        Task.FromResult(_agents.TryGetValue(agentId, out var a) ? a : null);

    public async Task<IAgent> GetOrCreateAsync(string agentId, Func<Task<(AgentType type, AgentConfiguration cfg)>> factory)
    {
        if (_agents.TryGetValue(agentId, out var existing)) return existing;

        var (type, cfg) = await factory();
        var agent = await _agentFactory.CreateAgentAsync(type, cfg); // inject IAgentFactory via ctor
        _agents[agentId] = agent;
        return agent;
    }

    public async Task RemoveAsync(string agentId)
    {
        if (_agents.TryRemove(agentId, out var agent))
        {
            if (agent is IAsyncDisposable ad) await ad.DisposeAsync();
            else if (agent is IDisposable d) d.Dispose();
        }
    }
}
```

AgentHub update (conceptual):
```csharp
// before: always created Claude with new AgentConfiguration()
// after:
var agent = await _registry.GetOrCreateAsync(request.AgentId, async () =>
{
    var stored = await _agentRepo.GetWithConfigurationAsync(request.AgentId)
                  ?? throw new InvalidOperationException($"Agent {request.AgentId} not found");
    var cfg = MapToCoreConfig(stored.Configuration); // build AgentConfiguration + CustomSettings
    return (stored.Type, cfg);
});
```

## Acceptance Criteria

- Creating an agent in the UI persists it and it appears after page reload.
- Sending a chat message to that agent ID uses the correct adapter (Claude or Saturn) and streams text.
- If the chosen provider prerequisites are missing (CLI or API key), the user receives a clear error message.
- SignalR no longer hardcodes Claude agent creation.

## Manual QA

- Claude path:
  - Install CLI, create “Claude” agent, send a message → observe streaming reply.

- Saturn path (OpenRouter):
  - Export `OPENROUTER_API_KEY`, create “Saturn” agent with provider OpenRouter, send a message → observe streaming reply.

- Persistence:
  - Restart the web app → agents list loads from DB → chat still works without recreating agents.

—

Notes:
- The repository already contains a rich Data layer: avoid duplicating SaturnFork persistence under `OrchestratorChat.Saturn`. Use `OrchestratorChat.Data` instead.
- Keep changes minimal and focused on aligning the runtime registry and hub wiring.

