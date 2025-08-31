# OrchestratorChat — User Guide

This guide walks you through installing, configuring, and using OrchestratorChat. It also calls out current limitations and reliable paths to get productive today.

## Overview

OrchestratorChat is a .NET 8 multi‑agent orchestration platform with:
- Blazor Server web UI (chat, sessions, agents)
- SignalR hubs for real‑time messaging
- Agent adapters: Claude (via Claude CLI) and Saturn (embedded, via OpenRouter/Anthropic)
- EF Core + SQLite persistence for sessions/messages (agents persistence is partially wired)

If you’re new, start with the web app and the Claude path (fastest to validate). Saturn works headlessly provided you configure an API key.

## Prerequisites

- .NET 8 SDK
- Windows 10/11 (primary target) or Linux/macOS
- For Claude agent:
  - Install the Claude CLI and sign in
  - Verify with `claude --version`
- For Saturn agent:
  - Set `OPENROUTER_API_KEY` (or configure Anthropic OAuth/API key per CLAUDE.md)

Optional environment variables (set per your shell):
- `ANTHROPIC_API_KEY` — if using Claude via API key flows
- `OPENROUTER_API_KEY` — if using Saturn with OpenRouter

## Quick Start

1) Build the solution
- `dotnet build`

2) Run the web app
- `cd src/OrchestratorChat.Web`
- `dotnet run`
- Open: `https://localhost:5001` (or `http://localhost:5000`)

3) Add an agent
- Go to “Dashboard” (home page)
- Click “Add Agent”
- Choose “Claude” for the most reliable path now
  - Make sure `claude` CLI is installed and authenticated
- Name your agent and Create

4) Start a session
- From “Dashboard”, click “New Session”
- Enter a session name, choose the type (Single Agent is fine)
- Create → you’ll be navigated to the session view

5) Chat with an agent
- From “Dashboard” click an agent card to open Chat, or go to the “Chat” page and select your agent from the sidebar
- Type a message and send — you should see streaming responses

Notes:
- The chat pipeline currently instantiates a Claude agent by default when sending messages via SignalR. If you don’t have the Claude CLI installed, sending messages will fail. Saturn support exists but requires follow‑up wiring (see Limitations and Planning below).

## Using the Console Client (optional)

A small console app can connect to the server and expose a lightweight HTTP API:
- Terminal 1: run the web app (as above)
- Terminal 2:
  - `cd src/OrchestratorChat.ConsoleClient`
  - `dotnet run -- --server https://localhost:5001 --api-port 8080 --agent claude-1 --session-name "Console Session"`

This will connect to the SignalR hubs, create/join a session, and listen for HTTP commands.

## Provider Configuration

Claude (CLI)
- Install the Claude CLI and sign in (OAuth is preferred)
- Verify with `claude --version`
- No other configuration required for the fast path

Saturn (OpenRouter)
- Export `OPENROUTER_API_KEY`
- Saturn agent configuration can read `CustomSettings` such as `Provider = OpenRouter` and `ApiKey` but will also fall back to `OPENROUTER_API_KEY` from the environment

Anthropic (for Saturn)
- See CLAUDE.md → “Environment Requirements” and “Anthropic Provider” sections
- You can use `ANTHROPIC_API_KEY` or OAuth; Saturn’s provider supports both patterns

## Troubleshooting

- “Claude CLI not found or not authenticated”
  - Ensure `claude` is installed and on PATH
  - Run `claude --version`, and sign in if needed
  - Restart the web app after installing/signing in

- “Nothing happens when I send a message”
  - The current SignalR AgentHub creates a Claude agent by default for new agent IDs
  - If Claude CLI isn’t installed, message handling fails silently/with errors in server logs

- “I created a Saturn agent but Chat still uses Claude”
  - Known limitation: Chat/SignalR path doesn’t yet use the persisted/selected agent type
  - Use the Claude path for now, or see the planning doc linked below for the integration work

- SignalR connection issues
  - Check TLS/ports 5000/5001
  - Look at the browser console for WebSocket errors
  - If you changed the server URL, update the Console Client’s `--server` option

- Database issues
  - The SQLite DB is created on first run
  - If broken, delete `orchestrator.db` and restart (dev only)

## Current Limitations (and Workarounds)

- The UI’s “Add Agent” stores agents in memory; the SignalR AgentHub maintains a separate in‑memory cache and, when needed, creates a Claude agent by default for message handling.
- Because of this mismatch:
  - The Chat page can list an agent you created, but SignalR may create a different runtime agent (Claude) when you send a message
  - To use Chat today reliably, install and sign in to the Claude CLI
  - Saturn can stream responses via its providers but requires the integration fixes described below

See: `docs/planning/agent-session-integration-plan.md` for the concrete wiring plan to align persistence, SignalR, and agent creation so Saturn/Claude are selected correctly.

## More Docs

- Technical deep‑dive and development guidance: `CLAUDE.md`
- Planning docs (agents/tools/providers/persistence): `docs/planning/`

