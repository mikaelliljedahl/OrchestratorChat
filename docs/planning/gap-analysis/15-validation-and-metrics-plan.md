# Validation & Metrics — Plan

## Validation Strategy
- Layered tests: unit (providers, verification, decisions), integration (SignalR hubs, agent flows), end-to-end (team plan to completion).
- Staged rollout with feature flags (Anthropic OAuth, decisions UI).
- Manual PO acceptance scripts (SOP-aligned scenarios) for each milestone.

## Metrics (MVP)
- Lead time per step/plan; approval counts and times; decision counts.
- Error rates (tool failures, hub reconnections, auth errors).
- Agent health (ready/busy/error), streaming latency.

## Dashboards (post-MVP)
- Timeline overlays for approvals/decisions
- Status panel with health + recent errors

## Acceptance Criteria
- Each milestone has automated tests covering its core behavior.
- Build/test pipelines run with clear pass/fail gating.
- Basic metrics visible via logs or lightweight UI panels.

