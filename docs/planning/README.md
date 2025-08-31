# OrchestratorChat Planning â€” Start Here

This is the canonical entry point for all planning docs. Use this index (not file names) as the source of truth. Older, legacy docs remain for reference and are explicitly marked below.

Status at a glance
- Milestone 1 (Saturn Anthropic OAuth UI): In Progress
- Agent lifecycle (repo + registry unification): Complete
- Claude CLI reliability on Windows: Complete
- Team formation / plan editor / decisions & approvals UI: Planned
- Data entities (team/decision/approval/handoff): Planned

Status tags
- [Current]: Actively used. Follow these.
- [Roadmap]: Direction and scope. Inform execution.
- [Reference]: Useful background. Not authoritative.
- [Legacy]: Superseded by newer docs. Do not implement from these without crossâ€‘checking.

Start Here (Read in order)
1) [Product Vision & Team Operating Model] (Current)
   - docs/planning/00-product-vision-and-team-operating-model.md
2) [Multiâ€‘Agent Collaboration Protocol] (Current)
   - docs/planning/01-multi-agent-collaboration-protocol.md
3) [Agent Roles & Capabilities Catalog] (Current)
   - docs/planning/02-agent-roles-and-capabilities-catalog.md
4) [Orchestration Modes & Decision Policies] (Current)
   - docs/planning/03-orchestration-modes-and-decision-policies.md
5) [Team Runbook & Scenarios] (Current)
   - docs/planning/07-team-runbook-and-scenarios.md

Roadmap & Execution
- [Roadmap â€” Milestones & Owners] (Roadmap)
  - docs/planning/gap-analysis/11-roadmap-milestones-and-owners.md
- [Engineering Checklist â€” From Plan to PRs] (Current)
  - docs/planning/gap-analysis/16-engineering-checklist.md
- [Acceptance Test Scenarios â€” MVP] (Current)
  - docs/planning/gap-analysis/17-acceptance-test-scenarios.md

Provider & Agent System (Saturn/Claude)
- [Saturn Anthropic OAuth â€” Feature Spec] (Current)
  - docs/planning/done/track3/06-saturn-anthropic-oauth-feature-spec.md
- [Provider Selection & Verification â€” UX/Service Plan] (Current)
  - docs/planning/track3/07-provider-selection-and-verification-plan.md

Gap Analysis docs to scope and the Checklist to slice PRs.

## Implementation Order (Remaining Work)
1) Blazor Service Architecture Refactor — docs/planning/done/04-blazor-service-architecture-refactor.md
2) Saturn Anthropic OAuth — docs/planning/done/track3/06-saturn-anthropic-oauth-feature-spec.md
3) Provider Selection & Verification — docs/planning/track3/07-provider-selection-and-verification-plan.md
4) Team & Plan skeleton wiring —
   - UI gaps: docs/planning/gap-analysis/12-ui-gaps-and-plans.md
   - Runtime wiring: docs/planning/gap-analysis/13-runtime-architecture-gaps-and-plans.md
   - Data gaps (if any): docs/planning/gap-analysis/14-data-model-and-persistence-gaps.md
5) Decision protocol & ADRs —
   - Concepts: docs/planning/03-orchestration-modes-and-decision-policies.md
   - Execution: docs/planning/gap-analysis/13-runtime-architecture-gaps-and-plans.md
6) Tool approvals & safety — docs/planning/gap-analysis/13-runtime-architecture-gaps-and-plans.md
7) Timeline & observability polish — docs/planning/gap-analysis/12-ui-gaps-and-plans.md, docs/planning/gap-analysis/15-validation-and-metrics-plan.md

Last updated: This index is maintained alongside planning changes. If you add a new plan/spec, update this file to keep the canonical order intact.





## Done
- Chat SignalR wiring fix: docs/planning/done/05-chat-signalr-wiring-fix.md
- Agent selection & defaulting fix: docs/planning/done/06-agent-selection-and-defaulting-fix.md
- Legacy/reference: docs/planning/done/legacy/




## Remaining Work (Pick Items)
- Team & Plan skeleton wiring (high)
  - Orchestrator page: add session scoping; call TeamPlanService.SaveTeamAsync/SavePlanAsync; add “Commit Plan”; display committed plan timeline
  - TeamSetup: bind to real agents via AgentService; save members with real AgentId/Name/Type/Role
  - PlanEditor: owners list from persisted Team; save PlanStep.Owner as AgentId; reload after save
  - Tests: TeamPlanService round-trip; Orchestrator page persistence/render
- Decision protocol foundations (medium)
  - Add Decision/ADR entities + migrations; define SignalR decision contracts; stub ADR write on commit
- Tool approvals scaffolding (medium)
  - Add Approval entity + migrations; hub request/response; ToolExecutor policy hook (feature flag)
- Timeline & observability polish (low)
  - Render plan/commit entries; add basic metrics placeholder
- ProviderFactory cleanup (small)
  - Typed factory methods/interfaces; DI logging; unit test


## Next Sprints (New Plans)
- Continuous Orchestration Loop: docs/planning/08-continuous-orchestration-loop.md
- Context Window Management: docs/planning/09-context-window-management.md
- Role Prompt Library: docs/planning/10-role-prompt-library.md


