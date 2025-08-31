# Agent Selection & Defaulting Fix — Chat UX Alignment

## Purpose
Ensure users can actually select an agent in Chat, and that a sensible default is selected automatically so they can start typing immediately after creation.

## Current Pain
- Agents are saved to the database, but Chat shows no selectable agent or starts with none selected.
- Users must click precisely, and there is no default selection on load.

## Target UX
- After creating an agent, it becomes the default (or the user is prompted to set a default).
- When opening Chat:
  - If a default agent exists, it is auto-selected.
  - Otherwise, the first available agent is selected.
- The sidebar agent list is obviously clickable, and selection changes enable the input area immediately.

## Changes (Services/UI)
- AgentService / Repository
  - Keep `GetDefaultAgentAsync()` and `SetDefaultAgentAsync(string id)` wired (already present).
  - On agent creation via Wizard/Create dialog, call `SetDefaultAgentAsync(newAgent.Id)` if no default exists.
- ChatInterface.razor
  - After `LoadAgents()`:
    - If `_selectedAgent` is null:
      - Try `AgentService.GetDefaultAgentAsync()` and set `_selectedAgent` to it if found.
      - Else, set `_selectedAgent = _agents.FirstOrDefault()`.
    - Call `StateHasChanged()` and, if using SignalR groups, subscribe (see 05-chat-signalr-wiring-fix.md).
  - When navigating to `/chat/{AgentId}`, if `AgentId` is provided, select that agent and subscribe.
- AgentSidebar.razor
  - Make the list item clearly clickable:
    - Use `MudListItem` with `T="AgentInfo"` and/or set the `Clickable`/`Button` style (or wrap with `MudButton`/`MudListItemLink`).
  - Keep `OnAgentSelected` raising with the selected `AgentInfo`.

## Optional Visual Aids
- Show a small checkmark on the default agent; add a “Set as default” action in the agent menu.

## Acceptance Criteria
- Opening Chat with existing agents shows one selected (default if set; otherwise first).
- Creating an agent via the wizard/dialog results in that agent being selected by default on the next Chat open.
- Clicking different agents updates selection and enables the message input.

## Tests
- Component tests: ChatInterface selects default/first agent when none is selected; selecting an agent invokes `OnAgentSelected` and enables input.
- Service tests: `SetDefaultAgentAsync` clears the previous default and sets the new one.
## Tests
- Component tests: ChatInterface selects default/first agent when none is selected; selecting an agent invokes `OnAgentSelected` and enables input.
- Service tests: `SetDefaultAgentAsync` clears the previous default and sets the new one.
