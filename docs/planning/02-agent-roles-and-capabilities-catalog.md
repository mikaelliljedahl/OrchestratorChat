# Agent Roles & Capabilities Catalog

## Purpose
Define the default roles, responsibilities, and capability requirements for agents so teams are formed with clear expectations.

## Roles
- Orchestrator (Facilitator)
  - Responsibilities: kickoff, plan commit, tie‑break, approvals routing
  - Capabilities: summarization, decision management, limited tools (read/greps)
- Planner (System Design)
  - Responsibilities: decompose goals into steps, acceptance criteria
  - Capabilities: analysis/summarization; read/grep/glob tools
- Coder (Implementation)
  - Responsibilities: write code/diffs, run commands, create tests
  - Capabilities: file read/write, apply diff, grep/glob, bash/command
  - Guardrails: command/file approvals per policy
- Researcher (Docs/API Investigation)
  - Responsibilities: gather references, external queries, synthesize findings
  - Capabilities: web fetch/search tools (if enabled); read/grep
- Reviewer/QA (Validation)
  - Responsibilities: verify acceptance criteria, run tests, request fixes
  - Capabilities: read/logs, grep, run tests, limited write for trivial fixes

## Capability Schema (example)
- supportsStreaming: bool
- supportsTools: bool
- availableTools: [name, parameters]
- supportedModels: [strings]
- maxTokens: number
- constraints: { allowWrite: bool, allowCommand: bool, reviewedBy: role }

## Tool Policy Templates
- Safe (read‑only): read_file, grep, glob
- Coding: + write_file, apply_diff, search_and_replace, bash (approval)
- Research: + web_fetch (approval), limited write for notes only
- QA: run tests, read logs, suggest diffs (approval to apply)

## Model/Provider Defaults
- Claude Path (CLI): Orchestrator/Planner/Reviewer
- Saturn + OpenRouter: Coder/Researcher (fast streaming)
- Saturn + Anthropic OAuth: Coder/Planner (secure enterprise path)

## Team Formation Heuristics
- Small tasks: Orchestrator + Coder
- Medium tasks: + Planner or Reviewer
- Research tasks: Orchestrator + Researcher (+ Coder for spike)

## Acceptance Criteria
- Team creation UI leverages roles to suggest composition
- Policies generated from role profiles and attached to plan steps
- Agents advertise capabilities; orchestration uses them for routing/handoffs

