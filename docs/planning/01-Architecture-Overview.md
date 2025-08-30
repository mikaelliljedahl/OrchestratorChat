# OrchestratorChat Architecture Overview

## Executive Summary
OrchestratorChat is a .NET-based multi-agent orchestration platform that enables simultaneous coordination of multiple AI agents including Claude Code, embedded Saturn, and future extensibility for other agents. Built with ASP.NET Core and Blazor Server, it provides real-time multi-agent collaboration with a web-based interface.

## System Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Web UI (Blazor Server)                  │
│  ┌─────────┐ ┌──────────┐ ┌────────────┐ ┌──────────────┐ │
│  │Dashboard│ │Agent Chat│ │Orchestrator│ │Session History│ │
│  └─────────┘ └──────────┘ └────────────┘ └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                    SignalR Real-time Layer
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Orchestration Engine                      │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐   │
│  │Task Scheduler│ │Message Router│ │Session Management│   │
│  └──────────────┘ └──────────────┘ └──────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      Agent Adapters                          │
│  ┌────────────┐ ┌────────────┐ ┌─────────────────────┐    │
│  │Claude Code │ │Saturn      │ │Future Agents        │    │
│  │Adapter     │ │(Embedded)  │ │(Plugin Architecture)│    │
│  └────────────┘ └────────────┘ └─────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                       Data Layer                             │
│  ┌──────────┐ ┌──────────────┐ ┌────────────────────┐     │
│  │SQLite DB │ │Configuration │ │MCP Configuration   │     │
│  └──────────┘ └──────────────┘ └────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
C:\code\github\OrchestratorChat\
├── src\
│   ├── OrchestratorChat.Core\           # Core abstractions and interfaces
│   ├── OrchestratorChat.Agents\         # Agent adapter implementations
│   ├── OrchestratorChat.Saturn\         # Embedded Saturn library
│   ├── OrchestratorChat.SignalR\        # Real-time communication
│   ├── OrchestratorChat.Web\            # Blazor Server application
│   ├── OrchestratorChat.Data\           # Data persistence layer
│   └── OrchestratorChat.Configuration\  # Configuration management
├── tests\
│   ├── OrchestratorChat.Core.Tests\
│   ├── OrchestratorChat.Agents.Tests\
│   └── OrchestratorChat.Web.Tests\
├── docs\
│   └── planning\                        # This documentation
├── OrchestratorChat.sln
└── README.md
```

## Technology Stack

### Core Technologies
- **.NET 8.0**: Latest LTS version for long-term support
- **ASP.NET Core**: Web framework for hosting
- **Blazor Server**: Real-time UI with C# instead of JavaScript
- **SignalR**: WebSocket-based real-time communication
- **Entity Framework Core**: ORM for data access
- **SQLite**: Embedded database for persistence

### Key Libraries
- **MudBlazor**: Material Design component library (reused from claudecodewrappersharp)
- **FluentValidation**: Input validation
- **Serilog**: Structured logging
- **AutoMapper**: Object mapping
- **Polly**: Resilience and retry policies

## Core Design Principles

### 1. Separation of Concerns
Each project has a single, well-defined responsibility. Dependencies flow inward toward the core.

### 2. Dependency Injection
All services are registered via DI container, enabling testability and loose coupling.

### 3. Asynchronous by Default
All I/O operations are async to maximize throughput and prevent blocking.

### 4. Real-time First
UI updates stream in real-time via SignalR without page refreshes.

### 5. Agent Agnostic
Core system doesn't know about specific agent implementations, only interfaces.

## Data Flow

### Request Flow
1. User interacts with Blazor UI component
2. Component invokes SignalR hub method
3. Hub delegates to orchestration engine
4. Engine routes to appropriate agent adapter(s)
5. Adapter translates to agent-specific protocol
6. Agent processes and returns response

### Response Flow
1. Agent generates output (potentially streaming)
2. Adapter translates to common format
3. Engine processes and potentially routes to other agents
4. SignalR streams updates to connected clients
5. Blazor components update UI in real-time
6. Data persistence layer stores conversation

## Security Considerations

### Authentication & Authorization
- Windows Authentication for corporate environments
- JWT tokens for API access
- Role-based access control for agent management

### Data Protection
- Encryption at rest for sensitive data
- TLS for all network communication
- Secure storage of API keys and credentials

### Process Isolation
- Agent processes run with limited permissions
- Sandboxed execution environment
- Resource limits to prevent abuse

## Scalability Architecture

### Horizontal Scaling
- Multiple orchestrator instances behind load balancer
- Redis backplane for SignalR scale-out
- Distributed session state

### Performance Optimization
- Response streaming to reduce latency
- Efficient message batching
- Connection pooling for database
- Lazy loading of historical data

## Integration Points

### MCP (Model Context Protocol)
- Import existing MCP configurations from claudecodewrappersharp
- Project-specific `.mcp.json` files
- Global tool registry
- Dynamic tool discovery

### External Systems
- REST API for third-party integration
- Webhook support for notifications
- Export capabilities (JSON, CSV)
- Plugin architecture for extensions

## Development Workflow

### Parallel Development Tracks

**Track 1: Core & Data (Developer 1)**
- Implement core abstractions
- Set up data layer
- Create configuration system

**Track 2: Agent Adapters (Developer 2)**
- Build Claude Code adapter
- Transform Saturn to embedded library
- Design plugin architecture

**Track 3: Web UI (Developer 3)**
- Set up Blazor project
- Port UI components from claudecodewrappersharp
- Implement real-time updates

**Track 4: SignalR & Orchestration (Developer 4)**
- Design SignalR hubs
- Build orchestration engine
- Implement message routing

## Success Metrics

### Performance Targets
- < 100ms latency for message routing
- Support 100+ concurrent sessions
- < 2 second agent initialization
- 99.9% uptime for core services

### Functional Requirements
- Multi-agent conversation support
- Session persistence and recovery
- Real-time streaming responses
- Cross-agent tool sharing

## Risk Mitigation

### Technical Risks
- **Agent Process Crashes**: Implement supervisor pattern with automatic restart
- **Memory Leaks**: Regular health checks and process recycling
- **Network Failures**: Retry policies with exponential backoff
- **Data Corruption**: Transaction support and backup strategies

### Architectural Decisions Log

| Decision | Rationale | Alternatives Considered |
|----------|-----------|------------------------|
| Blazor Server | Real-time updates, no JS complexity | Blazor WASM, React |
| Embedded Saturn | Better performance, easier integration | Microservice, REST API |
| SQLite | Simple deployment, sufficient for use case | PostgreSQL, SQL Server |
| SignalR | Native .NET, bidirectional streaming | gRPC, WebSockets |

## Next Steps
1. Review and approve architecture
2. Set up development environment
3. Create project structure
4. Begin parallel development tracks
5. Weekly architecture sync meetings

## Document Control
- **Version**: 1.0
- **Date**: 2024-01-30
- **Author**: Architecture Team
- **Review**: Pending
- **Approval**: Pending