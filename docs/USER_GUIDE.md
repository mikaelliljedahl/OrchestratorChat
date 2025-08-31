# OrchestratorChat — User Guide

This guide walks you through installing, configuring, and using OrchestratorChat. It also calls out current limitations and reliable paths to get productive today.

## Overview

OrchestratorChat is a .NET 8 multi‑agent orchestration platform with:
- Blazor Server web UI (chat, sessions, agents)
- SignalR hubs for real‑time messaging
- Agent adapters: Claude (via Claude CLI) and Saturn (embedded, via OpenRouter/Anthropic)
- EF Core + SQLite persistence for sessions/messages (agents persistence is partially wired)

If you’re new, start with the First‑Run Wizard in the web app. Pick Claude (if you have the CLI) or Saturn (OpenRouter). The wizard verifies your setup and leads you straight into a chat.

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

3) First‑Run Wizard (first launch)
- The app opens a short wizard:
  - Choose a provider: Claude CLI or Saturn (OpenRouter)
  - Verify provider: auto‑detect Claude CLI or paste/validate OpenRouter API key
  - Name & defaults: prefilled sensible options
  - Create & test: agent is created and a quick test runs in the background

4) Start Chat
- Click “Start Chat” at the end of the wizard (or use “New Chat” from the top bar later)
- A session is created automatically; you land in Chat ready to send a message

5) Chat with an agent
- Type a message and send — you should see streaming responses

Notes:
- The setup wizard verifies your provider before chat. If anything is missing (e.g., Claude CLI or OpenRouter key), the UI shows a single fix action.

## Using the Console Client (optional)

A small console app can connect to the server and expose a lightweight HTTP API:
- Terminal 1: run the web app (as above)
- Terminal 2:
  - `cd src/OrchestratorChat.ConsoleClient`
  - `dotnet run -- --server https://localhost:5001 --api-port 8080 --agent claude-1 --session-name "Console Session"`

This will connect to the SignalR hubs, create/join a session, and listen for HTTP commands.

## Provider Configuration

Claude (CLI)
- Install the Claude CLI and sign in (OAuth preferred)
- Verify with `claude --version`
- The wizard auto‑detects it and guides you if not found

Saturn (OpenRouter)
- Paste your `OPENROUTER_API_KEY` in the wizard (or export it beforehand)
- Saturn will also fall back to the environment variable if present

Anthropic (for Saturn)
- See CLAUDE.md → “Environment Requirements” and “Anthropic Provider” sections
- You can use `ANTHROPIC_API_KEY` or OAuth; Saturn’s provider supports both patterns

## Troubleshooting

- “Claude CLI not found or not authenticated”
  - Click the “Install Claude CLI” fix in the Health panel
  - Ensure `claude` is on PATH; verify with `claude --version`

- “Nothing happens when I send a message”
  - Check Health panel for provider or connection issues; follow the single fix action

- “I created a Saturn agent but Chat still uses Claude”
  - This is addressed by the onboarding alignment work (see planning docs). If not yet deployed in your build, use Claude CLI path or follow `docs/planning/agent-session-integration-plan.md`.

- SignalR connection issues
  - Check TLS/ports 5000/5001
  - Look at the browser console for WebSocket errors
  - If you changed the server URL, update the Console Client’s `--server` option

- Database issues
  - The SQLite DB is created on first run
  - If broken, delete `orchestrator.db` and restart (dev only)

## Health & Diagnostics Panel

- Chat header shows compact status badges for provider, hubs, and agent state
- Expandable panel appears when an error blocks sending; it offers exactly one primary fix action
- Once resolved, the panel collapses automatically

## Current Limitations

- If the wiring changes in `docs/planning/agent-session-integration-plan.md` are not yet applied in your build, prefer the Claude path to avoid SignalR defaulting to Claude unexpectedly. The onboarding docs in `docs/planning/ux-onboarding` describe the intended fixes and UI.

## More Docs

- Technical deep‑dive and development guidance: `CLAUDE.md`
- Planning docs (agents/tools/providers/persistence): `docs/planning/`
