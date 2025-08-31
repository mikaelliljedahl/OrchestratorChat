# UI Gaps & Plans

## Gaps
- Team Formation: No UI to compose a team (roles, agent selection) per session.
- Plan Authoring: No step editor with owners, acceptance criteria, and tool policies.
- Decisions & ADRs: No panels for propose/vote/commit or ADR visualization.
- Approvals: No consistent approval prompts for tools/commands/diffs.
- Provider Selection: Saturn treated as OpenRouter-only; missing Anthropic OAuth connect/status.
- Timeline: Lacks consolidated view of plan/steps, handoffs, decisions, approvals.

## Plans
- Session Setup Screen
  - Add Team panel: choose agents by role; persist to session.
  - Add Plan panel: step list with owners, acceptance, tool policy (safe/coding/qa presets).
- Chat Enhancements
  - CreateAgentDialog: provider sub-choice (OpenRouter vs Anthropic); connect status.
  - Decision widget: render prompts, options, vote buttons, and result banner.
  - Approval banner: show request with details; approve/decline → route to SignalR.
- Timeline Component
  - Render plan created, step start/end, decisions, approvals, handoffs, errors.
  - Filter and grouping by event type.
- Provider Wizard Update
  - Add Anthropic OAuth branch and status; remove OpenRouter gating when Anthropic chosen.

## Acceptance Criteria
- PO can configure team + plan before execution.
- Decisions and approvals are actionable in UI; outcomes visible in timeline.
- Saturn + Anthropic UI path requires no OpenRouter key.

