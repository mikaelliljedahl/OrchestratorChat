# Chat Start Flow — Simple, Predictable

Goal: Make “Start Chat” the most obvious action and remove unnecessary dialogs.

## Entry Points
- Global “New Chat” button (top bar)
  - If default agent set → open Chat with that agent
  - If no default agent → open First‑Run Wizard (or agent creation dialog)
- Agent card primary action: “Chat” → opens Chat with that agent

## Implicit Session Creation
- On entering Chat without an active session:
  - Call OrchestratorHub `CreateSession` with the chosen agent ID
  - Join session group; subscribe to agent group
  - Display chat header with agent name + health badges

## Messaging Flow
- User sends message → AgentHub `SendAgentMessage`
- Responses stream in real‑time
- On any blocking error, render inline error with recommended fix (see Health doc)

## Acceptance Criteria
- New Chat is always one click when a default agent exists
- Sessions are created automatically without modal friction
- Users see streaming output or a single, clear error

