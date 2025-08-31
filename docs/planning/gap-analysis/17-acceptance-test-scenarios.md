# Acceptance Test Scenarios — MVP

Scenarios validate end-to-end capabilities aligned with the product vision.

## A1: Claude Path — Single Agent Chat
- Pre: Claude CLI installed and detected
- Steps: Create Claude agent → Start session → Send/receive streaming replies
- Verify: Messages persisted; agent status Ready; no errors

## A2: Saturn + OpenRouter — Single Agent Chat
- Pre: OPENROUTER_API_KEY set
- Steps: Create Saturn(OpenRouter) agent → Start session → Send/receive streaming replies
- Verify: Headers include OpenRouter key; messages persisted

## A3: Saturn + Anthropic OAuth — Single Agent Chat
- Pre: None
- Steps: Wizard → Choose Saturn→Anthropic → Connect to Anthropic → Create Saturn agent → Send message
- Verify: Status connected; AnthropicProvider uses Authorization: Bearer; no OpenRouter prompts

## B1: Team Formation & Plan Commit
- Steps: Create session → Select team (Orchestrator, Coder, Reviewer) → Author plan (steps with owners, acceptance) → Commit
- Verify: Team and plan persisted; plan visible in timeline

## B2: Execute Plan with Approvals
- Steps: Start execution → Coder step triggers write/command → UI approval prompt → Approve → Step completes
- Verify: Approval record persisted; tool output summarized; step status updated

## B3: Decision Protocol
- Steps: Planner proposes decision (two options) → Vote (majority) → Commit → ADR recorded
- Verify: ADR shows rationale, participants; plan updated if needed

## C1: Handoff
- Steps: Owner requests handoff → Reviewer accepts → Owner changes → Execution resumes
- Verify: Handoff record persisted; timeline shows ownership change

## C2: Error Handling
- Steps: Induce provider/network error → System surfaces friendly error → Retry path works
- Verify: Error logged; no token leakage; user-facing copy clear

