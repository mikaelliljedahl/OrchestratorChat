# Copy Spec — Wizard, Health, and Chat

Short, direct, friendly. Avoid jargon and long blocks.

## Wizard
- Title: “Set up your first agent”
- Step 1 heading: “Choose a provider”
  - Claude CLI card subtitle: “Fastest path if Claude CLI is installed”
  - Saturn (OpenRouter) subtitle: “Use your OpenRouter API key”
- Step 2 heading: “Verify provider”
  - Claude status: “Claude CLI is detected.” / “Claude CLI not found.”
  - OpenRouter key field label: “OpenRouter API key”
  - CTA: “Validate” / disabled until non‑empty
- Step 3 heading: “Name & defaults”
  - Name label: “Agent name”
  - Model label: “Model (recommended default)”
  - Toggle: “Set as default agent”
  - Advanced accordion: “Advanced settings”
- Step 4 heading: “Create & test”
  - Progress text: “Creating agent…” then “Testing a quick message…”
  - Success: “All set! Start chatting.”
  - CTA: “Start Chat”

## Health Panel
- Header: “Connection & setup”
- Claude CLI: “Detected” / “Not found”
- OpenRouter key: “Present” / “Missing”
- OrchestratorHub: “Connected” / “Disconnected”
- AgentHub: “Connected” / “Disconnected”
- Agent status: “Ready” / “Error initializing”
- Fix CTAs:
  - “Install Claude CLI”
  - “Add API key”
  - “Retry initialization”
  - “Reconnect”

## Chat
- Input placeholder: “Send a message…”
- Typing indicator: “{AgentName} is thinking…”
- Error inline: “Can’t send yet — {reason}. {Action}”

