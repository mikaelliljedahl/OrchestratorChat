# Continuous Orchestration Loop & Supervisor

## Purpose
Keep multi-agent work going 24/7 until goals are reached. Detect stalls/false 'done', recover from agent failures, and restart the lead agent with a standard prompt to resume progress.

## Goals
- Add a supervisor loop that:
  - Monitors session progress, agent health, and step completion
  - Detects stalls (no progress over T, where TT = 5 minutes), early 'done', or crashed agents
  - Restarts the lead agent with a standard role prompt and a fresh/trimmed context
  - Escalates to PO when thresholds exceeded (optional)

## Non-Goals (this milestone)
- Full project management (dependencies, resourcing across repos)
- Advanced scheduling across machines

## Design
- SupervisorService (hosted service)
  - Poll interval configurable (e.g., 30–60s)
  - Inputs: Session state (current plan/steps), EventBus stream, AgentRegistry health, token usage/hints
  - Outputs: Orchestrator commands (restart, requeue step), Notifications (SignalR/PO)
- Supervisor heuristics
  - Stall: no new messages/tool runs/step progress for N minutes
  - False 'done': step/plan marked done but acceptance criteria not met (simple check: tests not run, missing artifacts, or open items list non-empty)
  - Agent crash/unhealthy: AgentStatus.Error or no heartbeats (AgentHealthMonitor)
- Actions
  - Restart lead agent: re-init with role prompt; inject brief session summary + next step goal
  - Requeue current step (optionally assign alternate owner)
  - Summon 'reviewer' agent to re-assess status (optional)
- Configuration
  - appsettings: Supervisor: { PollSeconds, StallMinutes, RetryBackoff, EscalationThreshold }

## APIs/Contracts (Core)
- ISupervisorService
  - Start/Stop (hosted)
  - EvaluateSession(sessionId)
- Orchestrator additions
  - RestartLeadAgent(sessionId, reason)
  - RequeueStep(sessionId, stepId, newOwnerId?)
- SignalR events
  - Supervisor:Status, Supervisor:ActionTaken (timeline entries)

## Persistence
- Add SupervisorEvent entity (sessionId, type, details, timestamp)
- Optional supervisor settings in ConfigurationService

## Acceptance Criteria
- When a stall or agent error is simulated, the supervisor restarts the lead agent with a standard prompt and work resumes
- Timeline shows 'Supervisor restarted lead agent (reason: …)' entries
- No false 'done': supervisor re-opens the step if acceptance not met

## Tests
- Unit: Supervisor heuristics (stall detection, false done) and action selection
- Integration: Simulated stall -> restart lead agent -> new messages arrive

## Risks
- Over-eager restarts; mitigate with backoff and thresholds
- False positives; provide an override/disable per session

---

## Next Steps (PR Slices)
1) Add SupervisorService + DI + configuration
2) Orchestrator APIs: RestartLeadAgent/RequeueStep; timeline events
3) Hook AgentHealthMonitor; add stall detection using EventBus timestamps
4) Unit/integration tests; basic UI timeline entries
