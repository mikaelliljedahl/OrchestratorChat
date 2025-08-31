# Saturn Anthropic OAuth — Feature Spec

## Overview
- Enable creating and using Saturn agents with Anthropic via OAuth (preferred), without requiring an OpenRouter API key.
- Provide a guided OAuth flow in the web UI, securely store tokens, and have the Saturn Anthropic provider automatically use them.

## Goals
- Add a clear “Connect to Anthropic” path in the UI (FirstRun wizard + Create Agent dialog).
- Handle the full OAuth flow (PKCE + state) in the web app and securely store tokens.
- Make Saturn’s Anthropic provider prefer OAuth Bearer tokens when available; fall back to `ANTHROPIC_API_KEY` if set.
- Surface connection status and token expiry in UI (read‑only) to inform users.

## Non‑Goals
- Do not remove the OpenRouter path; it remains fully supported.
- Do not implement advanced token management UI beyond connect/logout and status.

## User Experience
- FirstRun Wizard
  - Provider selection: shows “Saturn” with two sub‑options: “OpenRouter (API Key)” and “Anthropic (OAuth)”.
  - If “Anthropic (OAuth)”: Step 2 shows a “Connect to Anthropic” button and status panel. After successful OAuth, proceed to agent configuration and creation.
- CreateAgentDialog (Chat sidebar)
  - If Agent Type = Saturn and Provider = Anthropic, show a “Connect to Anthropic” button and a read‑only status indicator (Connected / Not connected).
  - Allow agent creation without blocking on runtime init; if tokens are missing, the agent is persisted but flagged “Connect Anthropic to enable.”
- Settings (optional future enhancement)
  - Add a Providers section to allow re‑checking status and triggering logout.

## API & Endpoints
- Controller: `src/OrchestratorChat.Web/Controllers/ProvidersController.cs`
  - `GET  /api/providers/anthropic/status`
    - Returns token presence, expiry, and scopes (no secrets).
    - Example response: `{ connected: true, expiresAt: "2025-09-30T12:34:56Z", scopes: ["user:inference"] }`
  - `POST /api/providers/anthropic/start`
    - Initiates OAuth: creates PKCE pair + state, stores in server‑side ephemeral store (MemoryCache keyed by state), and returns the authorize URL.
    - UI opens returned URL in new tab.
  - `GET  /oauth/anthropic/callback?code=&state=`
    - Validates state + retrieves stored code_verifier; exchanges code for tokens via AnthropicAuthService (web‑safe version).
    - Saves tokens using TokenStore; redirects to a small Razor page that closes itself and notifies opener via JS.
  - `POST /api/providers/anthropic/logout`
    - Clears stored tokens.

Notes
- For Blazor Server, use an in‑memory (scoped or singleton) PKCE/state store. Tie state to the current user/session to mitigate CSRF.
- All endpoints must enforce HTTPS and never return tokens to clients.

## Token Storage & Security
- Storage via existing `TokenStore` under `src/OrchestratorChat.Saturn/Providers/Anthropic/TokenStore.cs` (DPAPI on Windows, AES‑GCM elsewhere).
- No tokens in logs; mask errors. Enforce PKCE + state. Avoid mixing bearer and API keys simultaneously; choose precedence.
- Provide a clear logout that deletes tokens from disk.

## Saturn Anthropic Provider Behavior
- File: `src/OrchestratorChat.Saturn/Providers/ILLMProvider.cs` (AnthropicProvider)
- InitializeAsync():
  - Attempt to load OAuth tokens via `TokenStore.LoadTokensAsync()`.
    - If valid or refreshable → set `Authorization: Bearer <access_token>` header; do NOT set `x-api-key` header.
    - Else, check `ANTHROPIC_API_KEY` env var → set `x-api-key` header.
    - If neither present → report missing credentials (graceful error surfaced to UI/SignalR).
- When using OAuth, handle refresh transparently via TokenStore before requests if `NeedsRefresh`.

## Provider Verification Service
- Extend `IProviderVerificationService` to include Anthropic OAuth status:
  - `Task<ProviderStatus> CheckAnthropicOAuthAsync()` → reads TokenStore and returns `Present` when valid tokens exist (or refresh succeeds), else `Missing`.
- Update FirstRun wizard to respect this status for Saturn Anthropic.

## Error Handling
- If callback exchange fails → redirect to a friendly error page with “Retry OAuth” action.
- If tokens expire and refresh fails → report “Disconnected” status; UI shows “Reconnect to Anthropic.”
- If user tries to send messages without tokens → SignalR returns a user‑friendly error advising to connect Anthropic.

## Telemetry & Logging
- Do not log tokens/scopes. Log minimal event markers: `AnthropicOAuthStart`, `AnthropicOAuthCallback`, `AnthropicOAuthStored`, `AnthropicOAuthError`.

## Acceptance Criteria
- Users can select Saturn + Anthropic in both the wizard and CreateAgentDialog.
- Clicking “Connect to Anthropic” performs OAuth and stores tokens securely.
- `GET /api/providers/anthropic/status` returns `connected: true` afterward.
- Creating a Saturn+Anthropic agent succeeds without requiring an OpenRouter key.
- Sending a message to a Saturn+Anthropic agent succeeds using `Authorization: Bearer` header when tokens exist; fails with a clear error when not.
- OpenRouter path remains unchanged and functional.

## Test Plan
- Unit
  - ProviderVerificationService.CheckAnthropicOAuthAsync() → Present/Missing based on TokenStore content.
  - AnthropicProvider.InitializeAsync() uses OAuth over API key when tokens exist; header selection verified.
- Integration
  - OAuth roundtrip: start → callback → status returns connected.
  - SignalR message to Saturn+Anthropic agent streams text when connected.
  - Missing tokens path returns friendly guidance via SignalR error.
- Security
  - PKCE/state enforcement; callback rejects mismatched state.
  - Tokens never logged or returned from endpoints.

## Implementation Map
- UI (Blazor):
  - `Components/FirstRunWizard.razor`: add Anthropic OAuth branch and status.
  - `Components/CreateAgentDialog.razor`: show Connect button + status when Saturn+Anthropic.
- API:
  - `Controllers/ProvidersController.cs`: add start/callback/logout/status endpoints for Anthropic.
- Services:
  - `IProviderVerificationService` + `ProviderVerificationService`: Anthropic OAuth status.
- Saturn:
  - `Providers/ILLMProvider.cs` (AnthropicProvider): load tokens, set Bearer header, refresh logic.

---

## Rollout
- Behind a feature flag if needed (e.g., `Features:AnthropicOAuth: true`).
- Document in USER_GUIDE after completion; add troubleshooting (callback blocked, refresh fails, etc.).

