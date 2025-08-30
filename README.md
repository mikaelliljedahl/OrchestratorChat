# OrchestratorChat

A multi-agent AI orchestration platform that coordinates multiple AI assistants (Claude, Saturn, etc.) to work together on complex tasks.

## 🚀 Quick Start

```bash
# Build the project
dotnet build

# Run the web application
cd src/OrchestratorChat.Web
dotnet run

# Open browser to http://localhost:5000
```

## 📋 Project Overview

OrchestratorChat enables simultaneous coordination of multiple AI agents through a web interface. Think of it as a conductor for an orchestra of AI assistants - each playing their part to achieve complex goals.

### Key Features
- **Multi-Agent Coordination**: Run multiple AI agents simultaneously
- **Real-time Chat Interface**: Blazor-based web UI with live updates
- **Session Management**: Track conversations and agent interactions
- **Tool Execution**: Agents can use tools (file operations, web search, etc.)
- **Orchestration Plans**: Define strategies for agent collaboration

## 🏗️ Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Web UI    │────▶│ Orchestrator│────▶│   Agents    │
│  (Blazor)   │     │   (Core)    │     │(Claude/Saturn)
└─────────────┘     └─────────────┘     └─────────────┘
       │                   │                    │
       └───────────────────┴────────────────────┘
                          │
                   ┌─────────────┐
                   │  Database   │
                   │  (SQLite)   │
                   └─────────────┘
```

## 📊 Current Status

### ✅ Completed (95%)
- **Core Infrastructure**: SessionManager, Orchestrator, EventBus
- **Agent System**: Claude and Saturn agents with full tool support
- **Web UI**: All components created, fixes applied
- **Data Layer**: Repository pattern with Entity Framework

### 🔧 In Progress (5%)
- Final integration testing
- SignalR real-time updates
- Minor UI polish

## 🛠️ Tech Stack

- **.NET 8.0** - Core framework
- **ASP.NET Core** - Web hosting
- **Blazor Server** - Interactive web UI
- **MudBlazor** - Material Design components
- **Entity Framework Core** - Data persistence
- **SQLite** - Local database
- **SignalR** - Real-time communication

## 📁 Project Structure

```
OrchestratorChat/
├── src/
│   ├── OrchestratorChat.Core/        # Business logic & interfaces
│   ├── OrchestratorChat.Data/        # Database & repositories
│   ├── OrchestratorChat.Agents/      # AI agent implementations
│   ├── OrchestratorChat.Saturn/      # Saturn integration
│   ├── OrchestratorChat.Web/         # Blazor web application
│   └── OrchestratorChat.SignalR/     # Real-time communication
├── tests/                             # Unit & integration tests
└── docs/planning/                     # Architecture documentation
```

## 🔑 Requirements

- .NET 8.0 SDK or later
- Claude CLI (for Claude agent)
- API keys for AI providers (OpenRouter/Anthropic)

## 🚦 Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/OrchestratorChat.git
   cd OrchestratorChat
   ```

2. **Set up API keys**
   ```bash
   # For Claude (OAuth flow available)
   export ANTHROPIC_API_KEY=your_key_here
   
   # For Saturn/OpenRouter
   export OPENROUTER_API_KEY=your_key_here
   ```

3. **Build and run**
   ```bash
   dotnet build
   cd src/OrchestratorChat.Web
   dotnet run
   ```

4. **Open browser**
   Navigate to `http://localhost:5000`

## 📈 Development Progress

| Component | Status | Progress |
|-----------|--------|----------|
| Core Services | ✅ Complete | 100% |
| Agent System | ✅ Complete | 100% |
| Web UI | ✅ Complete | 100% |
| Data Layer | ✅ Complete | 100% |
| SignalR | 🔄 In Progress | 70% |
| Testing | 🔄 In Progress | 40% |

## 🤝 Contributing

This is an active development project. See [CLAUDE.md](CLAUDE.md) for detailed technical documentation and coding guidelines.

## 📝 Notes

- **For AI assistants**: See [CLAUDE.md](CLAUDE.md) for comprehensive technical details
- **Known issues**: Some compilation warnings remain but don't affect functionality
- **Database**: SQLite database is created automatically on first run

## 🎯 Next Steps

1. Complete SignalR hub implementations
2. Add comprehensive test coverage
3. Implement authentication system
4. Deploy to cloud hosting

---

*Last Updated: August 30, 2025*
*Version: 1.1 (Active Development)*