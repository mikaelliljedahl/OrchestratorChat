# Runtime Architecture — Gaps & Plans

## Gaps
- Team/Plan execution semantics are in Orchestrator but not wired to UI-driven teams/steps.
- Decision protocol not represented in hubs/messages; no integration with Orchestrator execution path.
- Tool approval pipeline not end-to-end (policy → prompt → hub → agent → result).
- Agent lifecycle duplication addressed, but ensure all controllers/hubs/services use registry+repo consistently.
- Saturn Anthropic token use not automatic; fallback logic and refresh missing.

## Plans
- Orchestrator Enhancements
  - Accept persisted Team + Plan from UI; validate agents and policies; commit plan.
  - Emit richer progress events (per-step owner, policy, approvals required) for timeline.
- SignalR Hubs
  - Add Decision events (Propose, Vote, Commit) and Approval requests/responses.
  - Route approvals to waiting tool execution; resume or abort accordingly.
- Agent Integration
  - Enforce tool policies in ToolExecutor path; request approval via hub when required.
  - Map agent errors and safety denials to user-friendly SignalR errors.
- Saturn Provider
  - Load OAuth tokens from TokenStore on init; refresh if needed; prefer Bearer vs API key.

## Acceptance Criteria
- Plans created in UI execute with correct owners and policies; approvals enforced.
- Decisions can pause/resume steps; ADR recorded.
- Saturn Anthropic works when connected; clear errors otherwise.

