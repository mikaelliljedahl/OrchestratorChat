Title: Saturn agent creation blocked by OpenRouter key; Anthropic OAuth path missing from UI

Summary
- Creating a Saturn agent currently requires entering an OpenRouter API key in the UI. However, Saturn also supports Anthropic via OAuth (preferred by users). The UI/verification flow does not expose an Anthropic OAuth path, and provider verification treats Saturn as OpenRouter-only. As a result, users who want Anthropic OAuth cannot create or use a Saturn agent without supplying an OpenRouter key.

Impact
- Blocks Saturn usage for users who prefer Anthropic OAuth.
- Confusing UX: CreateAgent dialog lists “Anthropic” as an option, but the wizard and verification still prompt for OpenRouter keys.
- Leads to failed creation or dead-ends, reducing user confidence.

Reproduction Steps
1) Launch Web app → open the First Run Wizard.
2) Choose Saturn. UI prompts “Enter your OpenRouter API key to continue,” with no Anthropic OAuth option.
3) Alternatively, open Chat → CreateAgentDialog → choose Agent Type: Saturn and Provider: Anthropic. Create appears to work, but:
   - Verification/health checks still expect OpenRouter key.
   - Runtime initialization may fail because Anthropic OAuth tokens are never collected.

Observed vs Expected
- Observed: Saturn path hard-requires OpenRouter API key; no OAuth flow offered. Anthropic selection in the Create dialog is not honored end-to-end.
- Expected: When provider = Anthropic, the UI should guide the user through Anthropic OAuth, store tokens securely, and allow agent creation and use without an OpenRouter key.

Root Causes (code-level)
- FirstRunWizard (src/OrchestratorChat.Web/Components/FirstRunWizard.razor): Saturn tile text and step 2 enforce OpenRouter key entry; no Anthropic OAuth branch.
- ProviderVerificationService (src/OrchestratorChat.Web/Services/ProviderVerificationService.cs):
  - Only checks/stores API keys (OpenRouter/Anthropic) and detects Claude CLI.
  - No OAuth-token presence/status check or OAuth initiation endpoints for Anthropic.
- Anthropic OAuth plumbing exists in Saturn (src/OrchestratorChat.Saturn/Providers/Anthropic/AnthropicAuthService.cs and TokenStore), but is designed for CLI/console interaction and is not wired to the web UI callback flow.
- Saturn provider resolution works (SaturnAgent → ISaturnCore.CreateProviderAsync), but AnthropicProvider (src/OrchestratorChat.Saturn/Providers/ILLMProvider.cs) does not automatically read tokens from TokenStore or an auth service; it expects _apiKey or _oauthToken to be present.

Mitigation Plan

1) UI: Add Anthropic OAuth path for Saturn
- CreateAgentDialog.razor: When Provider = “Anthropic”, hide OpenRouter key inputs entirely; show a “Connect to Anthropic” button that triggers OAuth.
- FirstRunWizard.razor:
  - Update Saturn section to present two sub-options: “OpenRouter (API Key)” and “Anthropic (OAuth)”.
  - For “Anthropic (OAuth)”, provide a step to start OAuth and reflect status (Connected/Not Connected) based on token presence.

2) Backend: OAuth endpoints and status
- Add providers controller endpoints (src/OrchestratorChat.Web/Controllers/ProvidersController.cs):
  - StartAnthropicOAuth: generates PKCE verifier/challenge + state, redirects to Anthropic authorize URL.
  - AnthropicOAuthCallback: receives code/state, exchanges for tokens via a web-safe version of AnthropicAuthService, stores tokens with TokenStore.
- Update ProviderVerificationService:
  - Add CheckAnthropicOAuthAsync() that loads TokenStore and reports Detected/Present/Missing based on stored tokens (valid or refreshable via TokenStore).
  - Expose a simple status DTO so UI can render “Connected to Anthropic” badges.

3) Saturn provider integration
- Refactor AnthropicProvider to load tokens automatically:
  - Inject an IAnthropicAuthService (web-safe interface) or read from TokenStore directly in InitializeAsync(). Prefer OAuth Bearer token when available; fall back to ANTHROPIC_API_KEY if present.
  - Ensure Authorization header uses Bearer when OAuth token exists (and drops x-api-key header).
- ISaturnCore.CreateProviderAsync should pass minimal settings; token handling should be encapsulated within AnthropicProvider to avoid leaking secrets across layers.

4) Agent creation logic
- When CreateAgentDialog posts a Saturn+Anthropic agent:
  - Persist agent immediately (done by AgentService), without requiring OpenRouter key.
  - Runtime initialization can proceed once tokens exist; surface a friendly “Connect Anthropic” banner if tokens are missing.

5) Health checks and gating
- Replace OpenRouter-only gating in wizard flow with provider-specific checks:
  - If Provider = OpenRouter → require API key validation (present + optional network check).
  - If Provider = Anthropic → require OAuth tokens present/valid; show “Connect Anthropic” if missing.

6) Tests
- Unit tests:
  - ProviderVerificationService.CheckAnthropicOAuthAsync returns Present when TokenStore has valid tokens.
  - AnthropicProvider.InitializeAsync uses OAuth token when present; falls back to API key; sets headers correctly.
- Integration tests (SignalR/Agents):
  - Creating Saturn agent with Provider=Anthropic and pre-seeded TokenStore → Send message succeeds and streams text.
  - Creating Saturn agent with Provider=Anthropic without tokens → Send returns a user-friendly error advising to connect Anthropic.

7) Documentation
- Update docs/USER_GUIDE.md with Anthropic OAuth flow (UI steps and troubleshooting).
- Add short admin notes on where tokens are stored and how to revoke (TokenStore.Logout).

Acceptance Criteria
- UI allows creating a Saturn agent with Provider=Anthropic without requesting an OpenRouter key.
- A “Connect to Anthropic” flow exists and stores tokens securely; status visible in UI.
- Sending a message to a Saturn+Anthropic agent uses OAuth (Authorization: Bearer) and succeeds when tokens are present.
- OpenRouter path remains fully supported and unchanged for users who prefer it.

Risks & Considerations
- OAuth callback flow in a Blazor Server app needs careful CSRF/state handling (use PKCE + state already present in Auth service).
- TokenStore path/permissions on Windows vs Linux/macOS should be verified in the hosted environment.
- Avoid logging tokens; scrub sensitive data in logs.

Effort Estimate
- UI changes: 0.5–1 day
- Backend endpoints + verification service updates: 0.5–1 day
- AnthropicProvider refactor + tests: 1–1.5 days
- QA and docs: 0.5 day

Rollback Plan
- If OAuth flow presents issues, keep the OpenRouter path as the default and hide OAuth behind a feature flag while iterating.

