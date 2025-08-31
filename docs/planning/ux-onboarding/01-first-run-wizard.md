# First‑Run Wizard — Specification

Purpose: Guide users from first launch to a working agent and their first chat in under 60 seconds.

## When It Appears
- Automatically when no agents exist in persistence.
- Accessible later from a global “New Chat” button if no default agent is set.

## Steps
1) Choose Provider
   - Provider cards with status badges:
     - Claude CLI — badge: Detected / Not found
     - Saturn (OpenRouter) — badge: Key set / Not set
   - Single‑selection.

2) Verify Provider
   - Claude: run detection (backend check) and show result inline
     - If not found, show a single CTA: “Install Claude CLI” (link)
   - OpenRouter: input for API key with masked entry and “Validate”
     - If present via env var/config, show as “Already set” with ability to override

3) Name & Defaults
   - Agent name (pre‑filled: “Claude (Local)” or “Saturn (OpenRouter)”) 
   - Model preset (recommended default per provider)
   - “Set as default agent” toggle (on by default)
   - Advanced (collapsed): temperature, max tokens, working directory

4) Create & Test
   - Create the agent (persist), pre‑warm runtime instance
   - Fire a background “Hello” test message and show streaming ticks
   - On success → “Start Chat” primary CTA
   - On failure → show clear error with exactly one fix action

## UI Components
- Modal or full‑page wizard with progress indicator
- Provider cards with health badges
- Inline validation messages (no toasts for critical blocking issues)
- Primary CTA text changes per step: Next → Validate → Create → Start Chat

## Data and Side‑Effects
- On completion, persist agent with full configuration and mark as default
- Instantiate runtime agent into shared registry for immediate chat
- Do not create a session here; session is created when starting chat

## Telemetry (optional)
- Track time to complete wizard, validation failures, aborts

## Acceptance Criteria
- If no agents exist, wizard shows and can be completed successfully via Claude or OpenRouter path
- After completion, clicking “Start Chat” lands in Chat with implicit session created and a streaming first response
- Errors during verification or creation surface a single clear next action

