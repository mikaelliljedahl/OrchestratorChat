# Data Model & Persistence — Gaps

## Current
- Entities: Sessions, Messages, Agents, AgentConfigurations, SessionAgents, ToolCalls, OrchestrationPlans, OrchestrationSteps, Attachments.

## Gaps
- Team entity missing (explicit team composition and role assignments per session).
- Decision/ADR persistence missing (topic, options, votes, commit, rationale).
- Tool Approvals: approval records (required/approved/denied, approver, timestamps) not modeled.
- Step ownership history/handoffs tracking missing.
- Metrics: per-step/plan durations, approvals count, decision counts for reporting.

## Plans
- Add entities:
  - Team (SessionId, Members[{AgentId, Role}], Policies)
  - Decision (SessionId, StepId?, Topic, Options, Votes, CommittedOption, Rationale, Participants, Timestamps)
  - Approval (SessionId, StepId?, AgentId, ToolName, InputHash, Required, Approved, ApprovedBy, ApprovedAt)
  - Handoff (SessionId, StepId, FromAgentId, ToAgentId, Reason, Timestamp)
  - Metrics snapshot tables (optional) or compute from events
- Index for common queries (by Session, Step, Agent, Timestamp).
- Migration scripts + seeding for local dev.

## Acceptance Criteria
- Persisted records exist for decisions, approvals, handoffs; queryable in UI.
- Team composition and role assignments are durable and versioned per session.

