# Multi‑Agent Collaboration Protocol

This spec defines how agents coordinate in OrchestratorChat to plan, decide, execute, and review work as a team. It maps product goals to technical contracts and component responsibilities.

## Entities
- Session: id, goal, constraints, timeline, snapshots
- Team: id, name, members (agents), roles, policies
- Agent: id, type, capabilities, tools, status
- Plan: id, steps[{id, owner, description, acceptance, toolPolicy, status}]
- Decision: id, topic, options, votes, committedOption, rationale, participants, timestamp
- Artifact: code diffs, docs, logs, test results

## Message Types
- Chat: user/assistant/tool messages (streamed via SignalR; persisted)
- Event: status changes, tool start/finish, handoffs, errors
- Decision: propose, vote, commit events; ADR summary
- Control: start/stop/pause session; approve/decline tool executions

## Routing (Current Components)
- OrchestratorHub (SignalR): session creation/join, orchestration messages, plan progress
- AgentHub (SignalR): agent‑direct messages, streaming responses, tool execution
- EventBus (Core): decoupled events between services (session, orchestration, agents)
- Data (EF): sessions, messages, agents, tools, plans, snapshots

## Collaboration States (per Session)
1) Initialized → TeamFormed → Planning → Executing → Reviewing → Completed/Cancelled/Error

## Default Workflow
1) Team Formation
   - Select agents by role/capabilities; persist composition on session
2) Planning
   - Planner drafts steps; teammates review; orchestrator commits plan
3) Execution Loop (per step)
   - Owner executes; uses tools per policy; streams updates
   - If blocked → raise Decision(topic=blocker), suggest options
   - Vote window or orchestrator decision
   - Handoff allowed if better owner is available
4) Review
   - Reviewer validates acceptance criteria; can reopen step
5) Wrap‑up
   - Persist ADRs, artifacts, summary; mark session complete

## Decision Protocol (baseline)
- Propose(topic, options, evidence)
- Deliberate(messages, references)
- Vote(eligible members)
- Commit(orchestrator tie‑break if needed)
- Record(decision as ADR in session timeline)

## Tool Use & Approval
- Tool policies per step:
  - Allowed tools list
  - Auto‑approve vs. require approval
  - Budget limits (time/tokens/files changed)
- Approvals managed via SignalR control messages; surfaced to UI for PO or orchestrator acceptance

## Persistence Requirements
- Save: team composition, plan/steps, decisions (ADRs), artifacts, tool executions, events
- Index: by session, agent, step, timestamps
- Snapshots: after major milestones (post‑plan, per step completion)

## Observability
- Timeline entries for: plan created, step start/complete, tool calls, handoffs, decisions, approvals
- Status endpoints for agents/sessions

## Safety & Guardrails
- Command/file write approval surfaces
- Diff size and sensitive file patterns require escalation
- Per‑agent sandbox (working directory) and rate limiting

## Mapping to Code (High‑Level)
- Team Service (new):
  - Compose team, assign roles, enforce policies
  - Store Team/Plan/Decision entities (reuse Data)
- Orchestrator (existing Core):
  - Add plan creation/commit hooks; execute steps with progress; emit events
- SignalR Hubs (existing):
  - Add Decision messages (propose/vote/commit)
  - Add Approval prompts for tool calls
- Data Layer (existing):
  - Ensure models support Plan/Step/Decision persistence (extend if needed)

## Acceptance Criteria
- Team can plan and execute a multi‑step task; decisions recorded as ADRs
- Tool calls honored by policy with approvals; denials logged with rationale
- Handoffs change step owner and continue execution seamlessly
- UI shows a coherent timeline of team activity and outcomes

