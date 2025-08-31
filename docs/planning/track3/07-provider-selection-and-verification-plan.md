# Provider Selection & Verification — UX/Service Plan

## Problem
- The current provider selection flow treats Saturn as OpenRouter-only, requiring an OpenRouter API key even when the user prefers Anthropic OAuth. Verification and gating logic do not account for Anthropic OAuth tokens.

## Goals
- Present provider choices clearly (Claude CLI, Saturn→OpenRouter, Saturn→Anthropic OAuth).
- Decouple gating rules: check OpenRouter key only when OpenRouter is chosen; check Anthropic OAuth tokens when Anthropic is chosen.
- Expose a simple provider status surface for UI badges.

## UX Changes
- FirstRunWizard.razor
  - Step 1: Choose Provider
    - Tiles: Claude CLI, Saturn with sub-options (OpenRouter / Anthropic OAuth).
  - Step 2: Verify Provider
    - If Claude: auto-detect CLI; show Install link if missing.
    - If Saturn→OpenRouter: input + validate key; show stored status.
    - If Saturn→Anthropic: show Connect button + status (Connected/Not Connected).
  - Step 3: Configure agent (name, model, etc.)
  - Step 4: Create & test
- CreateAgentDialog.razor
  - If Saturn+Anthropic: show Connect and status; do not block creation if disconnected (agent persists as “requires connect”).

## Service Layer
- `IProviderVerificationService`
  - Add: `Task<ProviderStatus> CheckAnthropicOAuthAsync()`
  - Add: `Task<ProviderStatusResponse> GetProviderStatusAsync()` returns:
    - `ClaudeCli: Detected|NotFound`
    - `OpenRouterKey: Present|Missing`
    - `AnthropicKey: Present|Missing` (API key)
    - `AnthropicOAuth: Present|Missing` (tokens)
- `ProviderVerificationService`
  - Implement CheckAnthropicOAuthAsync(): read TokenStore; return Present if valid tokens exist or can be refreshed.
  - Keep OpenRouter/Anthropic API key code as-is; do not conflate with OAuth.

## API Endpoints
- See OAuth feature spec for Anthropic endpoints.
- Add lightweight status endpoint:
  - `GET /api/providers/status` → combines all statuses for efficient UI refreshes.

## Gating Rules
- Claude CLI path: Next enabled when CLI detected.
- Saturn→OpenRouter: Next enabled when API key present (stored or env).
- Saturn→Anthropic: Next enabled when OAuth tokens present.
- Always allow “Skip” for advanced users; show clear warnings if skipping provider setup.

## Error Messages (UI copy)
- Anthropic OAuth: “Not connected. Click ‘Connect to Anthropic’ to sign in.”
- OpenRouter: “API key missing. Enter your key to continue or switch to Anthropic.”
- Claude CLI: “Not detected. Install from claude.ai/cli or switch to Saturn.”

## Acceptance Criteria
- Wizard and Create dialog reflect correct provider branch with appropriate gating.
- `GET /api/providers/status` reports accurate, up-to-date status for all providers.
- Anthropic OAuth Connected status appears immediately after successful callback.
- No OpenRouter prompts shown when Anthropic is chosen.

## Test Plan
- Unit tests for ProviderVerificationService combining statuses and new AnthropicOAuth status.
- UI tests (component-level) verifying the right panels render per provider selection.
- Manual end-to-end: connect Anthropic → status flips to Connected → agent create succeeds without OpenRouter key.

## Implementation Map
- UI: FirstRunWizard.razor, CreateAgentDialog.razor
- Service: IProviderVerificationService, ProviderVerificationService
- API: ProvidersController → `/api/providers/status` (aggregated)
- TokenStore: used indirectly; do not expose in UI beyond status.

