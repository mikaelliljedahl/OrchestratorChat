# Sprint Backlog — 2025-09-01 ? 2025-09-14

Sprint Goal
- Finish M1 (OAuth) securely/idiomatically and make M2 (Team/Plan) usable end-to-end, enabling a visible multi-agent plan with owners.

References
- Planning index: docs/planning/README.md
- Blazor refactor (providers): docs/planning/04-blazor-service-architecture-refactor.md
- OAuth feature & provider verification: docs/planning/track3/06-*.md, docs/planning/track3/07-*.md
- Team/Plan wiring: docs/planning/gap-analysis/12-*.md, 13-*.md, 14-*.md
- Acceptance scenarios: docs/planning/gap-analysis/17-acceptance-test-scenarios.md (A3, B1)

## Story 1 — Secure OAuth + DI Refactor (High)
- Description: Complete Saturn Anthropic OAuth with server-side PKCE/state and DI services; thin controllers; remove intra-app HTTP from components.
- Tasks:
  - Implement IAnthropicOAuthService, IOAuthStateStore; register in DI
  - Move logic from ProvidersController to service; keep /start, /callback, /status, /logout as thin wrappers
  - Enforce PKCE/state: generate/store at /start; validate at /callback; remove/deprecate insecure /exchange
  - Update components (FirstRunWizard/CreateAgentDialog) to inject service; no intra-app HttpClient; remove JS eval
  - ProviderFactory: replace object CreateClient with typed methods; DI logging
  - Tests: unit (start/callback/status), integration (start?callback?status; no token leakage)
- DoD:
  - A3 passes (Anthropic OAuth chat uses Bearer; no OpenRouter prompt)
  - /start and /callback enforce PKCE/state; /exchange removed or strictly validated
  - Components call DI services; controllers are thin

## Story 2 — Team & Plan Skeleton E2E (High)
- Description: Persist team with real agents; persist plan with owners; commit and display.
- Tasks:
  - Session scoping in Orchestrator page; pass sessionId
  - TeamSetup: list real agents via AgentService; add members by AgentId/Name/Type/Role
  - Orchestrator: OnTeamSaved ? TeamPlanService.SaveTeamAsync(sessionId, team) then reload
  - PlanEditor: owners from persisted Team members; OnPlanSaved ? SavePlanAsync(sessionId, plan) then reload
  - Add "Commit Plan" to set CommittedAt; show a simple committed plan panel/timeline
  - Tests: TeamPlanService round-trip; Orchestrator page saves/reads back and renders
- DoD:
  - B1 passes (create team with roles; draft plan with owners; commit; visible in UI)

## Story 3 — Decision Protocol Foundations (Medium)
- Description: Lay groundwork for propose/vote/commit and ADRs (no full UI yet).
- Tasks:
  - Data: add Decision/ADR entities; migrations
  - SignalR: define decision events (Propose, Vote, Commit) contracts (no UI wire yet)
  - Orchestrator: stub handlers to record ADRs on commit
  - Tests: data model persistence for Decision/ADR
- DoD:
  - Entities + contracts available; ADR written on a simulated commit

## Story 4 — Tool Approvals Scaffolding (Medium)
- Description: Add approvals model and hub messages; enforce policy in ToolExecutor (behind feature flag).
- Tasks:
  - Data: Approval entity; migrations
  - SignalR: approval request/response messages
  - Agents: ToolExecutor checks policy; awaits approval hook (no full UI yet)
  - Tests: approvals persistence; hub message contract serialization
- DoD:
  - Policy enforcement stubbed; approvals recorded; feature-flagged

## Story 5 — Timeline & Observability (Low)
- Description: Minimal timeline polish for plan/commit entries; basic metrics panel placeholder.
- Tasks:
  - Render plan created/commit entries in Orchestrator page
  - Add placeholder metrics panel (steps count, committedAt)
  - Tests: none required beyond visual/manual this sprint
- DoD:
  - Visible plan/commit entries; metrics placeholder shows basic counts

## Out of Scope This Sprint
- Full decision/voting UI
- Full approvals UI and diff/command safety thresholds
- Advanced timeline filtering/metrics

## Risks & Mitigations
- OAuth correctness ? server-side PKCE/state + unit tests
- DI consistency ? remove ad hoc LoggerFactory; use DI
- UI coupling ? service boundary for components; no intra-app HttpClient

## Acceptance (Sprint)
- A3 passes using DI + PKCE
- B1 passes with persisted team & plan and basic commit visibility
- ProviderFactory typed; controllers thin; components use services

