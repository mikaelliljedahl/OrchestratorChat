# Development Setup Guide

## Prerequisites

### Required Software
- **.NET 8.0 SDK** or later: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (17.8+) or **VS Code** with C# extension
- **Node.js** (18.x or later): [Download](https://nodejs.org/)
- **Git**: [Download](https://git-scm.com/)
- **Claude CLI**: [Installation Guide](https://docs.anthropic.com/claude-code)
- **SQLite**: Included with .NET SDK

### Optional Tools
- **Docker Desktop**: For containerized development
- **Postman**: For API testing
- **Azure Data Studio**: For database management
- **Windows Terminal**: Enhanced command-line experience

## Initial Project Setup

### 1. Create Solution Structure

```powershell
# Create solution directory
mkdir C:\code\github\OrchestratorChat
cd C:\code\github\OrchestratorChat

# Create solution file
dotnet new sln -n OrchestratorChat

# Create projects
dotnet new classlib -n OrchestratorChat.Core -o src/OrchestratorChat.Core
dotnet new classlib -n OrchestratorChat.Agents -o src/OrchestratorChat.Agents
dotnet new classlib -n OrchestratorChat.Saturn -o src/OrchestratorChat.Saturn
dotnet new classlib -n OrchestratorChat.SignalR -o src/OrchestratorChat.SignalR
dotnet new blazorserver -n OrchestratorChat.Web -o src/OrchestratorChat.Web
dotnet new classlib -n OrchestratorChat.Data -o src/OrchestratorChat.Data
dotnet new classlib -n OrchestratorChat.Configuration -o src/OrchestratorChat.Configuration

# Create test projects
dotnet new xunit -n OrchestratorChat.Core.Tests -o tests/OrchestratorChat.Core.Tests
dotnet new xunit -n OrchestratorChat.Agents.Tests -o tests/OrchestratorChat.Agents.Tests
dotnet new xunit -n OrchestratorChat.Web.Tests -o tests/OrchestratorChat.Web.Tests

# Add projects to solution
dotnet sln add src/OrchestratorChat.Core/OrchestratorChat.Core.csproj
dotnet sln add src/OrchestratorChat.Agents/OrchestratorChat.Agents.csproj
dotnet sln add src/OrchestratorChat.Saturn/OrchestratorChat.Saturn.csproj
dotnet sln add src/OrchestratorChat.SignalR/OrchestratorChat.SignalR.csproj
dotnet sln add src/OrchestratorChat.Web/OrchestratorChat.Web.csproj
dotnet sln add src/OrchestratorChat.Data/OrchestratorChat.Data.csproj
dotnet sln add src/OrchestratorChat.Configuration/OrchestratorChat.Configuration.csproj
dotnet sln add tests/OrchestratorChat.Core.Tests/OrchestratorChat.Core.Tests.csproj
dotnet sln add tests/OrchestratorChat.Agents.Tests/OrchestratorChat.Agents.Tests.csproj
dotnet sln add tests/OrchestratorChat.Web.Tests/OrchestratorChat.Web.Tests.csproj
```

### 2. Set Up Project References

```powershell
# Core references (no dependencies)
# Core is the foundation

# Data references
dotnet add src/OrchestratorChat.Data reference src/OrchestratorChat.Core

# Configuration references
dotnet add src/OrchestratorChat.Configuration reference src/OrchestratorChat.Core

# Saturn references
dotnet add src/OrchestratorChat.Saturn reference src/OrchestratorChat.Core

# Agents references
dotnet add src/OrchestratorChat.Agents reference src/OrchestratorChat.Core
dotnet add src/OrchestratorChat.Agents reference src/OrchestratorChat.Saturn

# SignalR references
dotnet add src/OrchestratorChat.SignalR reference src/OrchestratorChat.Core
dotnet add src/OrchestratorChat.SignalR reference src/OrchestratorChat.Agents

# Web references (references all)
dotnet add src/OrchestratorChat.Web reference src/OrchestratorChat.Core
dotnet add src/OrchestratorChat.Web reference src/OrchestratorChat.Agents
dotnet add src/OrchestratorChat.Web reference src/OrchestratorChat.Saturn
dotnet add src/OrchestratorChat.Web reference src/OrchestratorChat.SignalR
dotnet add src/OrchestratorChat.Web reference src/OrchestratorChat.Data
dotnet add src/OrchestratorChat.Web reference src/OrchestratorChat.Configuration

# Test references
dotnet add tests/OrchestratorChat.Core.Tests reference src/OrchestratorChat.Core
dotnet add tests/OrchestratorChat.Agents.Tests reference src/OrchestratorChat.Agents
dotnet add tests/OrchestratorChat.Web.Tests reference src/OrchestratorChat.Web
```

### 3. Install NuGet Packages

```powershell
# Core packages
dotnet add src/OrchestratorChat.Core package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/OrchestratorChat.Core package Microsoft.Extensions.Logging.Abstractions
dotnet add src/OrchestratorChat.Core package System.Text.Json

# Data packages
dotnet add src/OrchestratorChat.Data package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/OrchestratorChat.Data package Microsoft.EntityFrameworkCore.Design
dotnet add src/OrchestratorChat.Data package Microsoft.EntityFrameworkCore.Tools

# Web packages
dotnet add src/OrchestratorChat.Web package MudBlazor
dotnet add src/OrchestratorChat.Web package Microsoft.AspNetCore.SignalR.Client
dotnet add src/OrchestratorChat.Web package Markdig
dotnet add src/OrchestratorChat.Web package Serilog.AspNetCore
dotnet add src/OrchestratorChat.Web package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/OrchestratorChat.Web package AutoMapper.Extensions.Microsoft.DependencyInjection

# Configuration packages
dotnet add src/OrchestratorChat.Configuration package Microsoft.Extensions.Configuration.Json
dotnet add src/OrchestratorChat.Configuration package Microsoft.Extensions.Options.ConfigurationExtensions

# Saturn packages (for embedded Saturn)
dotnet add src/OrchestratorChat.Saturn package Polly
dotnet add src/OrchestratorChat.Saturn package System.Threading.Channels

# Test packages
dotnet add tests/OrchestratorChat.Core.Tests package Microsoft.NET.Test.Sdk
dotnet add tests/OrchestratorChat.Core.Tests package xunit
dotnet add tests/OrchestratorChat.Core.Tests package xunit.runner.visualstudio
dotnet add tests/OrchestratorChat.Core.Tests package Moq
dotnet add tests/OrchestratorChat.Core.Tests package FluentAssertions

dotnet add tests/OrchestratorChat.Web.Tests package bunit
dotnet add tests/OrchestratorChat.Web.Tests package Microsoft.AspNetCore.Mvc.Testing
```

## Configuration Files

### 1. appsettings.json (Web project)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=orchestrator.db",
    "Redis": "localhost:6379"
  },
  "Claude": {
    "ExecutablePath": "claude",
    "DefaultModel": "claude-3-sonnet-20240229",
    "EnableMcp": true
  },
  "Saturn": {
    "DefaultProvider": "OpenRouter",
    "MaxSubAgents": 5,
    "SupportedModels": [
      "claude-3-opus-20240229",
      "claude-3-sonnet-20240229",
      "gpt-4-turbo-preview",
      "deepseek-coder"
    ]
  },
  "SignalR": {
    "KeepAliveInterval": 15,
    "ClientTimeoutInterval": 30,
    "MaximumReceiveMessageSize": 102400,
    "EnableDetailedErrors": true
  },
  "Security": {
    "RequireAuthentication": false,
    "JwtSecret": "your-secret-key-at-least-32-characters-long!!",
    "TokenExpirationMinutes": 60
  },
  "AllowedHosts": "*"
}
```

### 2. appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  },
  "SignalR": {
    "EnableDetailedErrors": true
  },
  "Security": {
    "RequireAuthentication": false
  }
}
```

### 3. launchSettings.json
```json
{
  "profiles": {
    "OrchestratorChat.Web": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Saturn Integration Setup

### 1. Clone and Prepare Saturn
```powershell
# Clone SaturnFork
git clone https://github.com/yourusername/SaturnFork.git C:\code\github\SaturnFork
cd C:\code\github\SaturnFork

# Create integration branch
git checkout -b orchestrator-integration

# Copy necessary files to OrchestratorChat.Saturn
# Use the Saturn Integration Guide for detailed steps
```

### 2. Remove Terminal.Gui Dependencies
Follow the Saturn Integration Guide (07-Saturn-Integration-Guide.md) for detailed transformation steps.

## Database Setup

### 1. Create Initial Migration
```powershell
cd src/OrchestratorChat.Web

# Install EF Core tools globally if not already installed
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate -p ../OrchestratorChat.Data -s .

# Update database
dotnet ef database update -p ../OrchestratorChat.Data -s .
```

### 2. Verify Database
```powershell
# Check that orchestrator.db was created
ls orchestrator.db

# Optionally, open in SQLite browser to verify schema
# You can use Azure Data Studio or DB Browser for SQLite
```

## MCP Configuration Import

### 1. Import from claudecodewrappersharp
```powershell
# Copy MCP configuration files if available
copy C:\code\github\claudecodewrappersharp\src\ClaudeCodeWrapperSharp.Web\config\*.json `
     C:\code\github\OrchestratorChat\src\OrchestratorChat.Web\config\
```

### 2. Create Default MCP Configuration
Create `src/OrchestratorChat.Web/config/mcp-config.json`:
```json
{
  "globalTools": [
    {
      "name": "filesystem",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem"],
      "enabled": true
    },
    {
      "name": "github",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}"
      },
      "enabled": false
    }
  ]
}
```

## Running the Application

### 1. Build Solution
```powershell
# Build entire solution
dotnet build

# Or build in Release mode
dotnet build -c Release
```

### 2. Run Tests
```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/OrchestratorChat.Core.Tests
```

### 3. Start Application
```powershell
# Navigate to Web project
cd src/OrchestratorChat.Web

# Run the application
dotnet run

# Or use watch mode for development
dotnet watch run

# Application will be available at:
# https://localhost:5001
# http://localhost:5000
```

## Development Workflow

### Team Development Assignments

#### Developer 1: Core & Data Layer
```powershell
# Work on Core abstractions
cd src/OrchestratorChat.Core
# Implement interfaces and models

# Work on Data layer
cd src/OrchestratorChat.Data
# Implement repositories and DbContext
```

#### Developer 2: Agent Adapters & Saturn
```powershell
# Work on Claude adapter
cd src/OrchestratorChat.Agents
# Implement ClaudeAgent

# Transform Saturn
cd src/OrchestratorChat.Saturn
# Remove Terminal.Gui and create library
```

#### Developer 3: Web UI
```powershell
# Work on Blazor components
cd src/OrchestratorChat.Web
# Port components from claudecodewrappersharp
# Create new UI components
```

#### Developer 4: SignalR & Orchestration
```powershell
# Work on SignalR hubs
cd src/OrchestratorChat.SignalR
# Implement real-time communication
```

### Git Workflow
```powershell
# Create feature branch
git checkout -b feature/core-abstractions

# Make changes and commit
git add .
git commit -m "feat: implement core agent abstractions"

# Push to remote
git push origin feature/core-abstractions

# Create pull request for review
```

## Troubleshooting

### Common Issues and Solutions

#### Issue: Claude CLI not found
```powershell
# Verify Claude installation
claude --version

# If not found, install Claude CLI
# Follow instructions at https://docs.anthropic.com/claude-code
```

#### Issue: Database migration fails
```powershell
# Remove existing database
rm orchestrator.db

# Recreate migrations
rm -rf ../OrchestratorChat.Data/Migrations
dotnet ef migrations add InitialCreate -p ../OrchestratorChat.Data -s .
dotnet ef database update -p ../OrchestratorChat.Data -s .
```

#### Issue: SignalR connection fails
```powershell
# Check if ports are available
netstat -an | findstr :5001

# Kill process using port if necessary
# Or change port in launchSettings.json
```

#### Issue: MudBlazor components not rendering
```powershell
# Ensure MudBlazor is properly configured in Program.cs
# Check _Imports.razor includes MudBlazor
# Verify MainLayout.razor includes MudBlazor providers
```

## Visual Studio Setup

### 1. Open Solution
- Open Visual Studio 2022
- File → Open → Project/Solution
- Navigate to `C:\code\github\OrchestratorChat\OrchestratorChat.sln`

### 2. Set Startup Project
- Right-click `OrchestratorChat.Web` in Solution Explorer
- Select "Set as Startup Project"

### 3. Configure Multiple Startup Projects (Optional)
- Right-click Solution → Properties
- Select "Multiple startup projects"
- Set OrchestratorChat.Web to "Start"

### 4. Install Extensions
- **Blazor Extensions**: For enhanced Blazor development
- **Web Essentials**: For web development tools
- **Git Extensions**: For Git integration

## VS Code Setup

### 1. Open Workspace
```powershell
code C:\code\github\OrchestratorChat
```

### 2. Install Extensions
- C# (Microsoft)
- C# Dev Kit
- Blazor Snippets
- SQLite Viewer
- GitLens

### 3. Configure tasks.json
Create `.vscode/tasks.json`:
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/OrchestratorChat.sln"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": [
        "test",
        "${workspaceFolder}/OrchestratorChat.sln"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "run",
      "command": "dotnet",
      "type": "process",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/OrchestratorChat.Web/OrchestratorChat.Web.csproj"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

### 4. Configure launch.json
Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/OrchestratorChat.Web/bin/Debug/net8.0/OrchestratorChat.Web.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/OrchestratorChat.Web",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

## Docker Setup (Optional)

### 1. Create Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrchestratorChat.sln", "./"]
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/OrchestratorChat.Web/OrchestratorChat.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrchestratorChat.Web.dll"]
```

### 2. Build and Run Docker Container
```powershell
# Build Docker image
docker build -t orchestratorchat .

# Run container
docker run -d -p 8080:80 -p 8443:443 --name orchestratorchat orchestratorchat

# View logs
docker logs orchestratorchat
```

## Continuous Integration Setup

### GitHub Actions Workflow
Create `.github/workflows/ci.yml`:
```yaml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore -c Release
    
    - name: Test
      run: dotnet test --no-build --verbosity normal -c Release
    
    - name: Publish
      run: dotnet publish src/OrchestratorChat.Web/OrchestratorChat.Web.csproj -c Release -o ./publish
    
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: orchestratorchat-build
        path: ./publish
```

## Production Deployment Checklist

- [ ] Update connection strings
- [ ] Configure JWT secret
- [ ] Enable authentication
- [ ] Set up SSL certificates
- [ ] Configure logging
- [ ] Set up monitoring
- [ ] Configure backups
- [ ] Update CORS settings
- [ ] Set production environment variables
- [ ] Run security scan
- [ ] Performance testing
- [ ] Load testing

## Next Steps
1. Complete initial setup
2. Assign team members to tracks
3. Set up CI/CD pipeline
4. Begin development sprints
5. Schedule daily standups
6. Plan code reviews

## Support Resources
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [MudBlazor Components](https://mudblazor.com/)

## Version History
- v1.0 - Initial setup guide
- Date: 2024-01-30
- Status: Ready for development