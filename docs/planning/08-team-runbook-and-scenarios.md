# Team Runbook & Scenarios

## Purpose
Provide practical, repeatable operating procedures and example scenarios to guide teams.

## SOPs (Standard Operating Procedures)
- Session Kickoff
  - Confirm goal, constraints, done definition
  - Select team and roles; set tool policies
  - Planner drafts plan (3–7 steps max for MVP)
- Step Execution
  - Owner announces intent; lists tools to use
  - Execute with streaming updates; request approvals when required
  - Post results; mark step done when acceptance criteria satisfied
- Handoff
  - Use a Handoff event: from, to, reason, context
  - New owner acknowledges before proceeding
- Decision Handling
  - Follow Proposal → Deliberation → Vote → Commit
  - Record ADR with rationale and consequences
- Wrap‑Up
  - Summarize outcomes, open issues, next steps
  - Archive artifacts and snapshots

## Scenarios
- Bugfix (Small)
  - Team: Orchestrator + Coder + Reviewer
  - Plan: reproduce → patch → test → review → summarize
- Feature Spike (Research)
  - Team: Orchestrator + Researcher + Planner
  - Plan: gather references → options → decision → prototype → summary
- Refactor + Tests (Medium)
  - Team: Orchestrator + Coder + Reviewer
  - Plan: scope → staged diffs → run tests → stabilize → review → merge notes

## Checklists
- Safety
  - Big diff? Dangerous command? Sensitive file? → approval required
  - Token/time budgets respected
- Observability
  - Step start/end logged; decisions recorded
  - Tool outputs summarized (avoid log spam)
- Handoffs
  - Context compiled (files, links, notes)
  - Ownership clear; no parallel ownership

## Acceptance Criteria
- Teams following SOPs can complete small/medium tasks reliably
- Timeline reflects SOP checkpoints and decisions
- Fewer stalls due to explicit handoffs and decision templates

