# OrchestratorChat Planning — Start Here

This is the canonical entry point for all planning docs. Use this index (not file names) as the source of truth. Older, legacy docs remain for reference and are explicitly marked below.

Status tags
- [Current]: Actively used. Follow these.
- [Roadmap]: Direction and scope. Inform execution.
- [Reference]: Useful background. Not authoritative.
- [Legacy]: Superseded by newer docs. Do not implement from these without cross‑checking.

Start Here (Read in order)
1) [Product Vision & Team Operating Model] (Current)
   - docs/planning/00-product-vision-and-team-operating-model.md
2) [Multi‑Agent Collaboration Protocol] (Current)
   - docs/planning/01-multi-agent-collaboration-protocol.md
3) [Agent Roles & Capabilities Catalog] (Current)
   - docs/planning/02-agent-roles-and-capabilities-catalog.md
4) [Orchestration Modes & Decision Policies] (Current)
   - docs/planning/03-orchestration-modes-and-decision-policies.md
5) [Team Runbook & Scenarios] (Current)
   - docs/planning/08-team-runbook-and-scenarios.md

Roadmap & Execution
- [Roadmap — Milestones & Owners] (Roadmap)
  - docs/planning/gap-analysis/11-roadmap-milestones-and-owners.md
- [Engineering Checklist — From Plan to PRs] (Current)
  - docs/planning/gap-analysis/16-engineering-checklist.md
- [Acceptance Test Scenarios — MVP] (Current)
  - docs/planning/gap-analysis/17-acceptance-test-scenarios.md

Provider & Agent System (Saturn/Claude)
- [Saturn Anthropic OAuth — Feature Spec] (Current)
  - docs/planning/track3/06-saturn-anthropic-oauth-feature-spec.md
- [Provider Selection & Verification — UX/Service Plan] (Current)
  - docs/planning/track3/07-provider-selection-and-verification-plan.md

Gap Analysis (Use to align work to product goal)
- [Product vs Implementation — Gap Assessment] (Current)
  - docs/planning/gap-analysis/10-product-vs-implementation-gap-assessment.md
- [UI Gaps & Plans] (Current)
  - docs/planning/gap-analysis/12-ui-gaps-and-plans.md
- [Runtime Architecture — Gaps & Plans] (Current)
  - docs/planning/gap-analysis/13-runtime-architecture-gaps-and-plans.md
- [Data Model & Persistence — Gaps] (Current)
  - docs/planning/gap-analysis/14-data-model-and-persistence-gaps.md
- [Validation & Metrics — Plan] (Current)
  - docs/planning/gap-analysis/15-validation-and-metrics-plan.md

Legacy / Reference
- Track 2 (reference only — superseded where conflicts arise):
  - docs/planning/track2/01-provider-implementation-plan.md (Legacy)
  - docs/planning/track2/02-tools-implementation-plan.md (Legacy)
  - docs/planning/track2/03-agent-system-plan.md (Legacy)
  - docs/planning/track2/05-data-persistence-plan.md (Legacy)
- General note: When in doubt, defer to the “Start Here” set and the Engineering Checklist.

How to use this index
- Newcomers should start with “Start Here” (top 5 docs), then consult the Roadmap and Engineering Checklist to pick up a workstream.
- Implementers working on providers should follow the Saturn/Provider docs above.
- For cross‑cutting work (decisions/approvals/timeline), use the Gap Analysis docs to scope and the Checklist to slice PRs.

Last updated: This index is maintained alongside planning changes. If you add a new plan/spec, update this file to keep the canonical order intact.

