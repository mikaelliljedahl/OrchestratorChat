# Product Vision & Team Operating Model

## Vision
- Build a practical multi‑agent collaboration system where specialized agents (coding, research, planning, QA) converse, coordinate, and decide as a team to deliver working software with minimal human intervention.
- The Product Owner (PO) sets goals; the system forms a capable team, plans work, executes with tools, and reports progress/outcomes in human‑friendly terms.

## Guiding Principles
- Autonomy with accountability: agents act independently but leave an auditable trail (timeline, decisions, diffs).
- Human‑in‑the‑loop by policy: approvals for risky actions (command/file writes) are policy‑driven.
- Least privilege: agents only access required tools and scopes per session.
- Deterministic handoffs: explicit ownership on each step; clear start/finish criteria.
- Observability first: status, events, and metrics available in real time.

## MVP Outcomes
- Create a session → define a team (1–3 agents) → agree on a plan → complete a small coding task end‑to‑end.
- Persist agents, sessions, messages, decisions, and tool executions.
- Stream results to the UI; allow the PO to approve or decline actions.

## Non‑Goals (MVP)
- No automatic production deployment.
- No cross‑repo orchestration; focus on one workspace.
- No long‑term memory beyond session snapshots.

## Core Concepts
- Session: time‑boxed container for goals, team, conversation, artifacts, decisions.
- Team: named group of agents with roles/policies.
- Roles: orchestrator (facilitator), planner, coder, researcher, reviewer/QA.
- Plan: ordered steps with owners, acceptance criteria, and tool permissions.
- Decision: a committed choice with rationale and sign‑offs (votes/thresholds).

## End‑to‑End Flow
1) Kickoff
   - PO sets session goal and constraints.
   - Team forms from available agents (either suggested or selected).
2) Planning
   - Planner drafts a plan; team reviews; orchestrator commits.
3) Execution Loop
   - Step owner executes, uses tools, posts intermediate results.
   - If blocked, raises decision or requests handoff.
4) Review
   - Reviewer validates acceptance criteria; raises fixes or approves.
5) Wrap‑up
   - Summarize outcomes, decisions, artifacts; produce a handover note.

## Decision Policies (default)
- Propose → Deliberate → Commit (or Escalate to PO)
- Quorum: simple majority among eligible agents; orchestrator breaks ties.
- Timeouts: if no quorum within T, orchestrator decides or defers to PO.

## Safety & Governance
- Tool policies: per‑step approvals, per‑agent budgets (time/tokens), rate limits.
- Sandboxing: write/file/command scoped to the session workspace.
- Red flags: dangerous commands, large diffs, exfiltration patterns → require PO approval.

## Observability
- Timeline: status changes, tool calls, diffs, decisions, handoffs.
- Health: agent status, streaming backpressure, error rates.
- Metrics: steps completed, approvals required, lead time.

## Deliverables (Docs + Features)
- Collaboration Protocol Spec (message types, routing, state machine).
- Roles & Capabilities Catalog (who does what; required tools/models).
- Orchestration Modes & Policies (leader‑led, round‑robin, vote‑based).
- Team Runbook (scenarios, SOPs, decision templates, handoff patterns).

## Acceptance Criteria (Product)
- Team can complete a small coding task (e.g., add a feature flag) without PO micromanagement.
- All significant actions are visible and attributable in the timeline.
- Decisions are recorded with rationale and outcomes.
- PO can pause/approve/abort at any time.

