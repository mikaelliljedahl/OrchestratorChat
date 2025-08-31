# Agent Creation UX — Persistence + Runtime Alignment

Ensure the agent the user creates is the one used at runtime.

## UX Requirements
- “Add Agent” dialog (and Wizard step) saves to the database, not just memory
- A “Set as default agent” toggle designates which agent is preselected for New Chat
- Agent cards show health badges and have a primary “Chat” action

## Runtime Behavior
- A singleton runtime registry (shared across Web and Hubs) caches initialized agents
- SignalR hubs resolve agents by ID via the registry; on miss, load from DB and create via AgentFactory
- No hardcoded defaults (remove AgentHub’s unconditional Claude creation)

## Mapping and Config
- Persist: Name, Type (Claude|Saturn), WorkingDirectory, Model, Temperature, MaxTokens
- CustomSettings (JSON map) for provider details: `Provider = OpenRouter|Anthropic`, `ApiKey` (if stored), etc.
- When instantiating:
  - Claude: no API key required for CLI; fail fast if CLI missing
  - Saturn: prefer API key from CustomSettings; fallback to env vars

## Acceptance Criteria
- Creating an agent persists it and immediately makes it available for chat
- Chat with that agent uses the correct provider; errors are surfaced via health panel
- Restarting the app preserves agents; Chat still works without re‑setup

## Related Plan
- See `docs/planning/agent-session-integration-plan.md` for deeper wiring details

