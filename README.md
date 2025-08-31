# OrchestratorChat

A multi-agent AI orchestration platform that coordinates multiple AI assistants (Claude, Saturn, etc.) to work together on complex tasks, inspired by: https://github.com/baryhuang/claude-code-by-agents

## Quick Start

```bash
# Build the project
dotnet build

# Run the web application
cd src/OrchestratorChat.Web
dotnet run

# Open browser to http://localhost:5000
```

## Project Overview

OrchestratorChat enables simultaneous coordination of multiple AI agents through a web interface. Think of it as a conductor for an orchestra of AI assistants - each playing their part to achieve complex goals.

## Product Vision & Operating Model

This project follows a clear product vision and team operating model to enable practical multi-agent collaboration with human-in-the-loop controls, observability, and safety. See the full document: docs/planning/00-product-vision-and-team-operating-model.md

- Vision: Specialized agents (coding, research, planning, QA) coordinate to deliver software with minimal supervision.
- Guiding Principles: Autonomy with accountability, policy-driven approvals, least privilege, deterministic handoffs, observability first.
- MVP Scope: Create a session, form a team, plan, execute, review, and wrap up with auditable artifacts.
- Non-Goals (MVP): No production deployment, no cross-repo orchestration, no long-term memory beyond snapshots.

### Current Features
- Multi-Agent Coordination: Run multiple AI agents simultaneously
- Real-time Chat Interface: Blazor-based web UI with live updates
- Session Management: Track conversations and agent interactions
- Tool Execution: Agents can use tools (file operations, web search, etc.)
- Orchestration Plans: Define strategies for agent collaboration
- SignalR Streaming: Real-time messaging and event routing
- Data Persistence: Entity Framework Core with SQLite
- Console Client: Headless operation via a standalone console client

### Planned Features
- Collaboration Protocol Spec: Formalize message types, routing, and state machine
- Roles & Capabilities Catalog: Define agent roles and required tools/models
- Orchestration Modes & Policies: Leader-led, round-robin, vote-based strategies
- Team Runbook: SOPs, decision templates, and handoff patterns
- Decision Policies: Propose / Deliberate / Commit with quorum and timeouts
- Safety & Governance: Policy-driven approvals, budgets, sandboxing, red-flag detection
- Observability Enhancements: Timeline details, health dashboards, and metrics
- Post-MVP Reach: Production deployment, cross-repo orchestration, long-term memory

Planning index (start here): docs/planning/README.md

## Current Status (human-readable)

- Core infrastructure: implemented (sessions, event bus, orchestrator baseline)
- Agent lifecycle: unified via repository + registry; agents persist first, then init
- Claude path: working (CLI process). Windows launcher/path handling improved
- Saturn path: providers integrated; OpenRouter supported; Anthropic OAuth UI flow planned/in progress (see planning index)
- Web UI: chat/dashboard functional; provider wizard and team/plan/decisions UI planned
- SignalR: agent/orchestrator hubs streaming; approvals and decision events planned
- Data: EF Core with sessions/messages/agents; team/decision/approval entities planned
- Tests: present; will expand for new features
- Console client: operational (headless)

Note: If you see older completion claims below, this section supersedes them.

## Architecture

- Web UI: Blazor Server app for chat, sessions, and dashboards
- Core Orchestrator: session lifecycle, planning, event bus
- Agents: Claude and Saturn integrations with tool execution
- Real-time: SignalR hubs for orchestrator and agents
- Data: Entity Framework Core with SQLite

## Testing & Integration
- Integration Tests: 50+ tests for SignalR functionality
- Console Client: Standalone client for headless operation
- Test Coverage: Hub, Service, and end-to-end scenarios

## Tech Stack

- .NET 8.0 - Core framework
- ASP.NET Core - Web hosting
- Blazor Server - Interactive web UI
- MudBlazor - Material Design components
- Entity Framework Core - Data persistence
- SQLite - Local database
- SignalR - Real-time communication

## Project Structure

```
OrchestratorChat/
  src/
    OrchestratorChat.Core/           # Business logic & interfaces
    OrchestratorChat.Data/           # Database & repositories
    OrchestratorChat.Saturn/         # Saturn integration
    OrchestratorChat.Web/            # Blazor web application
    OrchestratorChat.SignalR/        # Real-time communication
    OrchestratorChat.ConsoleClient/  # Headless console client
  docs/planning/                     # Architecture & product docs
```

## Requirements

- .NET 8.0 SDK or later
- Claude CLI (for Claude agent)
- API keys for AI providers (OpenRouter/Anthropic)

## Getting Started

1. Clone the repository
   ```bash
   git clone https://github.com/yourusername/OrchestratorChat.git
   cd OrchestratorChat
   ```

2. Set up API keys
   ```bash
   # For Claude (OAuth flow available)
   export ANTHROPIC_API_KEY=your_key_here

   # For Saturn/OpenRouter
   export OPENROUTER_API_KEY=your_key_here
   ```

3. Build and run
   ```bash
   dotnet build
   cd src/OrchestratorChat.Web
   dotnet run
   ```

4. Open browser
   Navigate to `http://localhost:5000`

## Development Progress (honest snapshot)

| Component         | Status        | Progress |
|-------------------|---------------|----------|
| Core Services     | In progress   | ~60%     |
| Agent System      | In progress   | ~70%     |
| Web UI            | In progress   | ~45%     |
| Data Layer        | In progress   | ~65%     |
| SignalR           | In progress   | ~55%     |
| Integration Tests | In progress   | ~35%     |
| Console Client    | Basic working | ~60%     |

Notes:
- See planning index (docs/planning/README.md) for current milestones. Milestone 1 (Saturn Anthropic OAuth) is close; Milestone 2 (Team/Plan skeleton) is partially wired.
- The “Current Status” section above supersedes any older completion claims.

## Contributing

This is an active development project. See `CLAUDE.md` for detailed technical documentation and coding guidelines.

## Notes

- For AI assistants: See `CLAUDE.md` for comprehensive technical details
- Known issues: Some compilation warnings remain but don't affect functionality
- Database: SQLite database is created automatically on first run

## Next Steps

1. System integration testing across all components
2. Performance optimization and load testing
3. Production deployment preparation
4. User documentation and guides

## Additional Resources

- Console Client: Run `OrchestratorChat.ConsoleClient --help` for headless operation to be used inside an interactive CLI session to hook up with the other agents
- Testing: Run `dotnet test` to execute all integration tests
- API Documentation: SignalR hubs available at `/hubs/orchestrator` and `/hubs/agent`

---

