# Roadmap — Milestones & Owners

## Milestone 1: Provider UX — Anthropic OAuth for Saturn (1–2 sprints)
- Owner: Web + Saturn Provider
- Deliverables:
  - UI: OAuth connect in FirstRun + CreateAgentDialog; status badges
  - API: start/callback/logout endpoints; secure TokenStore integration
  - Saturn: AnthropicProvider uses OAuth bearer tokens; fallback to API key
  - Docs/tests: USER_GUIDE, unit/integration tests
- Exit Criteria: Create Saturn+Anthropic agent without OpenRouter; send message succeeds

## Milestone 2: Team Formation & Planning (1 sprint)
- Owner: Web + Core Orchestrator
- Deliverables:
  - Team selection UI (roles, agents); persist team on session
  - Plan drafting UI with step owners and acceptance criteria
  - Orchestrator validates/commits plan; surfaces to timeline
- Exit Criteria: Users can form a team, create a plan, and see plan in timeline

## Milestone 3: Decision Protocol & ADRs (1–2 sprints)
- Owner: Web + SignalR + Data
- Deliverables:
  - Decision events via SignalR (propose/vote/commit)
  - Voting panel UI; quorum/timeout policies
  - Persist ADRs; render in timeline
- Exit Criteria: Decisions visible with rationale; vote/commit flows work

## Milestone 4: Tool Approvals & Safety (1 sprint)
- Owner: Web + SignalR + Agents
- Deliverables:
  - Policy model per plan step (allowed tools, approvals)
  - Approval prompt UI → hub → agent execution; denial handling
  - Diff/command safety gates (thresholds → escalate)
- Exit Criteria: Tool runs require approvals per policy; denials logged with rationale

## Milestone 5: Timeline & Observability Polish (0.5–1 sprint)
- Owner: Web + SignalR
- Deliverables:
  - Unified timeline with plan/steps, decisions, approvals, handoffs, errors
  - Status overlays for agents/sessions; health metrics
- Exit Criteria: PO sees coherent team activity without digging logs

## Milestone 6: Hardening & Docs (0.5 sprint)
- Owner: All
- Deliverables:
  - Error copy, retries/backoff, cleanup flows
  - Security sweep (token handling, logs)
  - Docs: Runbook, troubleshooting, examples
- Exit Criteria: MVP readiness checklist passes

