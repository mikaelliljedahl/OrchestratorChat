# Orchestration Modes & Decision Policies

## Modes
- Facilitator‑Led: Orchestrator assigns steps, commits plan/decisions.
- Round‑Robin: Step ownership rotates among agents with matching capabilities.
- Vote‑Based: Any agent can propose; quorum commits; orchestrator tie‑breaks.

## Decision Lifecycle
- Trigger: blocker, ambiguous requirement, risky change, or competing approaches.
- Proposal: options with pros/cons, effort, risks.
- Deliberation: evidence gathering (researcher), impact analysis (planner).
- Vote: eligible members cast votes; quorum determines outcome.
- Commit: orchestrator records ADR; update plan/owner if needed.

## ADR Template (stored per decision)
- Title, Context, Options, Decision, Rationale, Consequences, Participants, Timestamp

## Policies
- Quorum default: simple majority of eligible voters.
- Timeouts: default 2 minutes deliberation; escalate to orchestrator on timeout.
- Safety gates: decisions involving commands/diffs over threshold require PO approval.

## Mapping to System
- Decisions as first‑class events (SignalR) + persistent records (EF).
- Orchestrator service publishes decision prompts; UI renders voting panel.
- After commit: Orchestrator updates the plan, assigns owners, and resumes execution.

## Acceptance Criteria
- Users can see decisions and final ADRs in the session timeline.
- Votes/participants are recorded; tie‑breaks are attributed to orchestrator.
- Safety‑gated decisions prompt for PO approval when required.

