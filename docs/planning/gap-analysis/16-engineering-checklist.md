# Engineering Checklist — From Plan to PRs

This checklist maps planning docs to concrete PR slices with owners, dependencies, and DoD.

## 1) Saturn Anthropic OAuth (Feature Spec: track3/06-saturn-anthropic-oauth-feature-spec.md)
- API
  - [ ] ProvidersController: POST /api/providers/anthropic/start (PKCE/state, authorize URL)
  - [ ] GET /oauth/anthropic/callback (state verify, token exchange, TokenStore save)
  - [ ] GET /api/providers/anthropic/status (connected, expiresAt, scopes)
  - [ ] POST /api/providers/anthropic/logout
- Service
  - [ ] ProviderVerificationService: CheckAnthropicOAuthAsync()
  - [ ] Aggregate status: GET /api/providers/status
- Saturn Provider
  - [ ] AnthropicProvider: load tokens, prefer Bearer, refresh if needed
  - [ ] Fallback to ANTHROPIC_API_KEY
- UI
  - [ ] FirstRunWizard: Anthropic OAuth branch + status
  - [ ] CreateAgentDialog: provider switch (OpenRouter vs Anthropic) + connect button
- DoD: Create Saturn+Anthropic agent; send message succeeds without OpenRouter key

## 2) Provider Selection & Verification (Plan: track3/07-provider-selection-and-verification-plan.md)
- [ ] Update status DTO: ClaudeCli, OpenRouterKey, AnthropicKey, AnthropicOAuth
- [ ] Wizard gating per provider branch; allow Skip with warning
- DoD: No OpenRouter prompts when Anthropic chosen; statuses reflect reality

## 3) Team Formation & Planning (Docs: 00, 01, 02)
- Data
  - [ ] Add Team entity (members, roles, policies)
  - [ ] Migrations
- UI
  - [ ] Session setup: team panel + plan editor (steps, owners, acceptance, tool policy preset)
- Orchestrator
  - [ ] Accept committed plan (owners/policies), validate, and execute
- DoD: Team + plan persisted; plan visible; execution uses owners/policies

## 4) Decision Protocol & ADRs (Docs: 01, 03)
- Data
  - [ ] Decision entity; ADR records
- SignalR
  - [ ] Decision events: Propose, Vote, Commit
- UI
  - [ ] Decision widget: options, votes, result banner
- DoD: Decisions recorded as ADRs; votes/participants visible; timeouts handled

## 5) Tool Approvals & Safety (Docs: 01, 03)
- Data
  - [ ] Approval entity (required/approved/denied, approver, timestamps)
- SignalR
  - [ ] Approval request/response messages
- Agents
  - [ ] Enforce policies in ToolExecutor; await approvals
- UI
  - [ ] Approval banners (approve/decline + details)
- DoD: Policies enforced; denials logged; large diffs/commands escalate

## 6) Timeline & Observability (Docs: 01, 08)
- [ ] Timeline render: plan/steps, decisions, approvals, handoffs, errors
- [ ] Status overlays; basic metrics panel
- DoD: PO sees coherent team activity without digging logs

## Testing & CI
- [ ] Unit: provider status, OAuth, decision reducer, approval handling
- [ ] Integration: hubs flow (message, decision, approval)
- [ ] E2E: team forms → plan → execute → review → wrap-up
- [ ] CI gates: dotnet test; minimal smoke on PR

