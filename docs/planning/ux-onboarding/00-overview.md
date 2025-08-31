# UX Onboarding — Overview and Goals

This work makes first‑run setup fast and obvious, and ensures that the agent a user creates is the one actually used in chat. No demo mode. The experience should guide the user to a successful first message with clear health feedback.

## Objectives
- Time‑to‑first‑response under 60 seconds from first launch.
- One clear path: create agent → validate provider → start chat.
- Health and error states are visible and actionable right where the user is.
- The agent selected in the UI is the one used by SignalR for messaging.

## Deliverables
- First‑Run Wizard (see 01-first-run-wizard.md)
- Provider Verification & Status (see 02-provider-verification.md)
- Agent Creation UX + Persistence alignment (see 03-agent-creation-ux.md)
- Chat Start Flow with implicit session creation (see 04-chat-start-flow.md)
- Health & Error Handling panel (see 05-health-and-error-handling.md)
- Copy and micro‑copy spec (see 06-copy-spec.md)

## Non‑Goals
- Introducing a demo/echo agent (explicitly out of scope).
- Advanced orchestration UI changes.
- Deep refactors beyond what’s needed to align agent creation and usage.

## Dependencies and Related Work
- Align runtime registry and persistence as per existing plan: `docs/planning/agent-session-integration-plan.md`.
- Use existing Data layer (EF Core + SQLite) for agent definitions; do not add a separate Saturn data namespace for this.

## Acceptance Criteria
- On first launch (no agents persisted), the wizard is shown and leads to an agent ready for chat.
- After wizard completion, opening Chat sends a message through the selected provider with a streaming reply.
- Health panel surfaces misconfigurations (e.g., missing CLI, missing API key) with a single, clear action to fix.
- Restarting the app shows the persisted agent and allows “New Chat” with no re‑setup.

## Rollout Plan
1) Implement Provider Verification backend checks
2) Implement First‑Run Wizard UI + flows
3) Wire Agent persistence + runtime registry + SignalR hub lookup
4) Add Health & Error panel to Chat
5) Polish copy and empty‑state guidance

