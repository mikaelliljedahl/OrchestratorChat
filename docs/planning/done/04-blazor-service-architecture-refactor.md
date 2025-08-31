# Blazor Service Architecture Refactor — Providers & Components

Status: Nearly complete (controllers thin; DI services in place). Remaining work below.

## Purpose
Make Blazor Server components call DI services directly instead of round‑tripping through our own controllers, while keeping controllers only for true HTTP edges (OAuth callbacks, public APIs). This reduces coupling, improves testability, and tightens security for OAuth.

## Current Pain
- Components call `ProvidersController` via HttpClient for intra‑app operations (start/status/exchange), duplicating logic and growing surface area.
- PKCE/state integrity is harder to guarantee when components participate in verifier/state handling.

## Target Architecture
- Components use DI services for provider operations.
- Controllers exist only for:
  - External redirects/callbacks (e.g., `GET /oauth/anthropic/callback`).
  - Public APIs (console client / external tools), when needed.
- One shared implementation handles all business logic (OAuth start/callback/status/logout, provider verification). Controllers and components both depend on it.

## New/Updated Services
- `IAnthropicOAuthService`
  - `Task<OAuthStartResult> StartAsync(HttpContext ctx)`
  - `Task<bool> HandleCallbackAsync(HttpContext ctx)`  // validates state & exchanges code using stored verifier
  - `Task<AnthropicOAuthStatus> GetStatusAsync()`
  - `Task LogoutAsync()`
- `IOAuthStateStore`
  - `Task StoreAsync(string userSessionKey, PkceState state)`  // code_verifier, state, createdAt
  - `Task<PkceState?> TryGetAsync(string userSessionKey, string state)`
  - `Task ClearAsync(string userSessionKey, string state)`
- `IProviderVerificationService` (extend)
  - `Task<ProviderStatus> CheckAnthropicOAuthAsync()`  // reads TokenStore
  - Aggregate `GetProviderStatusAsync()` should include `AnthropicOAuth`

## Refactor Plan (Files)
- Move business logic from `src/OrchestratorChat.Web/Controllers/ProvidersController.cs` into `IAnthropicOAuthService`.
- Update controllers to thin wrappers:
  - `POST /api/providers/anthropic/start` → `IAnthropicOAuthService.StartAsync`
  - `GET /oauth/anthropic/callback` → `IAnthropicOAuthService.HandleCallbackAsync`
  - `GET /api/providers/anthropic/status` → `IAnthropicOAuthService.GetStatusAsync`
  - `POST /api/providers/anthropic/logout` → `IAnthropicOAuthService.LogoutAsync`
- Update components to DI services (no intra‑app HttpClient):
  - `src/OrchestratorChat.Web/Components/FirstRunWizard.razor` → inject `IAnthropicOAuthService`, call `StartAsync` (returns authorize URL), open with JS; then poll `GetStatusAsync`.
  - `src/OrchestratorChat.Web/Components/CreateAgentDialog.razor` → same as above; remove manual code exchange or route it through the service which uses the stored verifier.
- Add `IOAuthStateStore` (in‑memory or distributed) to hold PKCE verifier + state per user/session.
- `AnthropicProvider` (already updated): prefer OAuth token; refresh and save; fallback to API key.
- `ProviderFactory` cleanup: remove object returns; use typed factory or DI.

## PKCE/State Integrity
- `/start`: server generates `state` + `code_verifier`; stores both via `IOAuthStateStore` keyed by user/session. Returns `authorizeUrl` only.
- `/callback`: server validates `state`; retrieves stored `code_verifier`; exchanges `code` for tokens; saves tokens (TokenStore). Clears used state.
- Manual code exchange (if kept): must use the stored verifier/state (service responsibility), never a client‑generated verifier.

## DI & Registration
- Register `IAnthropicOAuthService`, `IOAuthStateStore` as scoped/singleton as appropriate.
- Controllers and components receive the same service via DI.
- Prefer `ILogger<T>` from DI; do not instantiate `LoggerFactory` manually.

## Acceptance Criteria
- Components no longer call our own controllers for intra‑app operations (except to open the OAuth authorize URL and receive the callback).
- PKCE/state validation occurs entirely on the server using `IOAuthStateStore`.
- `GET /api/providers/anthropic/status` (if kept) returns the same data as `IAnthropicOAuthService.GetStatusAsync()`.
- FirstRunWizard/CreateAgentDialog show connected status and do not require OpenRouter when Anthropic is chosen.

## Remaining Work (pick these)
- ProviderFactory cleanup
  - Replace `object CreateClient(string, Dictionary<string,object>)` with typed factory methods or interfaces
  - Remove inline `new LoggerFactory()`; use DI `ILogger`/`ILoggerFactory`
- Unit test for ProviderFactory behavior after refactor
- Move this doc to `docs/planning/done/` once the above two are merged

## Tests
- Unit
  - `IAnthropicOAuthService.StartAsync` stores verifier/state correctly; returns valid authorize URL.
  - `HandleCallbackAsync` validates state, exchanges code, saves tokens; failure paths covered.
  - `ProviderVerificationService.CheckAnthropicOAuthAsync` → Present/Missing per TokenStore.
- Integration
  - End‑to‑end OAuth start→callback→status with mocked HTTP (no token leakage).
  - Component tests with mocked service (no HttpClient to our own endpoints).

## Risks & Mitigations
- State drift: Ensure state/verifier cleared after use; expire stale entries.
- Browser popup blockers: If `window.open` blocked, display the `authorizeUrl` as a link fallback.
- Future split (front/back): The service boundary remains stable; if split occurs, implement a thin client behind the same interface.

## PR Slices (Checklist)
1) Add `IOAuthStateStore`, `IAnthropicOAuthService` + implementation; register in DI.
2) Move ProvidersController logic into service; thin the controller.
3) Update FirstRunWizard/CreateAgentDialog to inject service; remove intra‑app HttpClient calls.
4) Clean up `ProviderFactory` typing/logging DI.
5) Tests: unit + integration for the above.
