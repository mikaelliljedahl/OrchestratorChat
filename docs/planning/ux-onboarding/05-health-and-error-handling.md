# Health & Error Handling — In-Context Diagnostics

Make problems obvious and solvable where the user is chatting. No overlays; concise, in-line panel.

## Health Checks
- Claude CLI: detection result (Detected / Not found)
- OpenRouter key: Present / Missing (if selected provider is OpenRouter)
- Anthropic key (optional): Present / Missing
- SignalR connections: OrchestratorHub + AgentHub Connected / Reconnecting / Disconnected
- Agent runtime: Initialized / Error (from initialization result)

## Severity & UI
- OK (green): no action
- Warning (amber): degraded but not blocking (e.g., reconnecting)
- Error (red): blocking; show a single next action

## Common Errors → Fixes
- Claude CLI not found → “Install Claude CLI” link; retry detection button
- OpenRouter key missing → masked input + “Save & Validate”
- Agent initialization failed → “View details” + “Retry initialization”
- Hub disconnected → “Reconnect” CTA; show backoff status

## Surfaces
- Chat header: compact badges (hover to expand)
- Expandable diagnostics panel below input when an error occurs

## Telemetry (optional)
- Count of blocking errors encountered pre‑first‑message
- Time from first error to resolution

## Acceptance Criteria
- When any blocking issue exists, the user sees exactly one primary fix action
- On success, the panel collapses and the user can send messages normally

