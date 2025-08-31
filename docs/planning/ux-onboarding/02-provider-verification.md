# Provider Verification — Backend + UI

Make provider status explicit and fixable. Verification runs in the background and exposes a simple status to the UI with a single corrective action if failing.

## Checks

Claude CLI
- Command: `claude --version` (Process start without shell; 5s timeout)
- Result mapping:
  - Exit 0 → Detected
  - Not found / non‑zero → Not found
- UI action: link to install/sign‑in docs (open in new tab)

OpenRouter
- Source: API key from stored agent config or `OPENROUTER_API_KEY`
- Basic validation: non‑empty string; optionally a HEAD/GET to `/models` with short timeout (if network errors are common, skip network validation and rely on key presence)
- UI action: masked input + “Save & Validate” button

Anthropic (for Saturn)
- Source: `ANTHROPIC_API_KEY` or OAuth (if present in future)
- Same approach as OpenRouter (presence check; optional network validation)

## API Surface (suggested)
- `GET /api/providers/status` → returns { claudeCli: Detected|NotFound, openRouterKey: Present|Missing, anthropicKey: Present|Missing }
- `POST /api/providers/openrouter/validate` → accepts key, stores securely, returns status

## UI States
- Provider cards show badge: Verified / Needs Attention
- Tooltip explains the result; click opens the fix action (install docs or key entry)

## Security
- Never log secrets
- Mask keys in UI; store securely server‑side

## Acceptance Criteria
- Provider badges in wizard and Agents page reflect actual status
- Attempting to chat with an unverified provider shows a clear in‑context fix suggestion

