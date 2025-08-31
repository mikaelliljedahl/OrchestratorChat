# Product vs Implementation — Gap Assessment

## Executive Summary
- Goal: multi‑agent team that plans, decides, executes, and reviews with minimal PO intervention.
- Current: solid foundations (agents, SignalR, sessions, data, basic orchestrator), but gaps in team formation, decision protocol, approvals UX, and provider UX (Saturn Anthropic OAuth).
- Priority gaps: (1) Provider UX (Anthropic OAuth), (2) Team + decision UX/protocol, (3) Unified agent lifecycle, (4) Tool approval flows, (5) Timeline/observability polish.

## Strengths (Already Implemented)
- Agents: Claude (CLI process) and Saturn (providers, tools) with streaming; AgentFactory/Registry in place.
- SignalR: OrchestratorHub/AgentHub with streaming and session creation; MessageRouter and StreamManager.
- Data: EF Core with Sessions/Messages/Agents/OrchestrationPlan/Steps models.
- Orchestrator: Plan creation and execution strategies scaffolding with progress reporting.
- Web UI: Blazor UI with chat, dashboard, first‑run wizard, basic health checks.

## Gaps by Pillar
- Multi‑Agent Teaming
  - No explicit “team” object/flow (roles, members, policies).
  - Session UI lacks team formation and plan review/commit mechanics.
- Decision Protocol
  - No end‑to‑end propose/vote/commit messages or UI panels; no ADR persistence and timeline entries.
- Provider UX
  - Saturn path hard‑gates on OpenRouter API key; Anthropic OAuth flow absent from UI.
- Agent Lifecycle
  - Residual duplication resolved partially; ensure hubs/services uniformly resolve agents via repository+registry; unify status.
- Tool Approvals & Safety
  - Policies are not consistently enforced with approval prompts in the UI; missing clear PO approval flow.
- Observability
  - Good events exist, but timeline/ADR/handoff/approvals presentation is not complete for a “team” view.
- Console/Headless
  - Works but lacks multi‑agent decision/approval semantics.

## Critical Path (Must‑Have for MVP)
1) Saturn Anthropic OAuth path + provider selection UX
2) Team formation + step planning UI and orchestration tie‑in
3) Decision protocol (propose/vote/commit) + ADR persistence and UI
4) Tool approval prompts end‑to‑end (policy→UI→hub→agent)
5) Timeline polish: decisions, handoffs, approvals, step transitions

## Nice‑to‑Have (After MVP)
- Team policy templates; role‑based tool budgets
- Advanced orchestration modes and quorum strategies
- Provider metrics and costs

## Risks & Mitigations
- OAuth complexity → Use PKCE/state and reuse TokenStore; incremental rollout with feature flag.
- Coordination complexity → Start with facilitator‑led mode + basic voting; expand later.
- UX bloat → Keep flows minimal: clear prompts, statuses, and a compact timeline.

