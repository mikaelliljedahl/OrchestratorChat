# Chat SignalR Wiring Fix — Agent/Session Subscriptions

## Purpose
Ensure Blazor Chat components actually receive streaming responses from agents by subscribing to the correct SignalR groups and/or session routing. Today, messages are broadcast to groups, but the UI doesn’t join them, so users don’t see agent replies.

## Current Pain
- AgentHub sends responses to:
  - `Clients.Group($"agent-{agentId}")`
  - `Clients.Group($"session-{sessionId}")`
- ChatInterface creates a HubConnection but does not call `SubscribeToAgent(agentId)` nor join the session group via OrchestratorHub.
- Result: Messages are sent, but the client is not in any target group → no replies in UI.

## Target Wiring
- When a user selects an agent (or opens Chat with an agent id), the Chat component must:
  1) Call AgentHub.SubscribeToAgent(agentId)
  2) Join the session group (via OrchestratorHub.JoinSession(sessionId)) if session context is available
- On dispose, unsubscribe/leave as needed.

## Changes (Components)
- `src/OrchestratorChat.Web/Pages/ChatInterface.razor`
  - On initialization or when `_selectedAgent` changes:
    - Call `_agentHub.InvokeAsync("SubscribeToAgent", _selectedAgent.Id)`.
    - If `_currentSession?.Id` is set and an OrchestratorHub connection exists, call JoinSession.
  - On dispose: `_agentHub.InvokeAsync("UnsubscribeFromAgent", _selectedAgent.Id)` and leave session group if applicable.
  - Optionally, add a small helper to re‑subscribe when navigating between agents.
- (Optional) Add an OrchestratorHub connection in Chat for session events and `JoinSession(sessionId)`.

## Changes (Hubs) — Optional Hardening
- Consider sending to `Clients.Caller` for the initial response in addition to groups, to reduce blank screens if subscription is delayed (keep groups as the primary channel).
- Add guardrails/logging if `SubscribeToAgent` is not called (warn, not error).

## Acceptance Criteria
- Selecting an agent causes UI to receive streaming `ReceiveAgentResponse` events for that agent.
- Opening a session chat joins the session group and receives session‑scoped responses.
- Switching agents resubscribes correctly; leaving chat unsubscribes.

## Tests
- Component test (bUnit or similar): selecting an agent triggers a hub invocation to `SubscribeToAgent`.
- Integration test (SignalR): client that subscribes receives `ReceiveAgentResponse`; unsubscribed client does not.

## Notes
- This fix aligns UI with existing group routing in AgentHub; no changes required to data or agents.
- Keep this wiring minimal and focused; it’s an unblocker for visible chat responses.

