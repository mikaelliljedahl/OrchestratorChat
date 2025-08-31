Title: Saturn OpenAI Provider — Design and Implementation Plan

Status: Implemented (initial), docs pending refinement

Summary:
- Add a first-class OpenAI provider to Saturn alongside OpenRouter and Anthropic.
- Use official OpenAI Chat Completions API with streaming, authenticated via `OPENAI_API_KEY`.
- Mirror Anthropic/OpenRouter provider patterns to minimize drift and ease maintenance.

Goals:
- Support streaming chat completions for OpenAI models (e.g., `gpt-5`, `gpt-5`, `gpt-5`).
- Add `ProviderType.OpenAI`, wire provider selection in `SaturnCore` and `ProviderFactory`.
- Provide a minimal `OpenAIClient` for consistency with existing client pattern.

Out of Scope (v1):
- Non‑streaming completion method implementation (throwing NotImplemented).
- Tool calls/functions and model discovery via OpenAI `/models`.
- “OpenAI Plus” web session usage. We use official API keys, not web sessions.

Implementation Details:
- Models: Reuse existing OpenRouter Chat Completions models for request/streaming chunk shapes.
- Streaming: Use shared `SseParser` to parse OpenAI’s `data: {json}` chunks ending with `[DONE]`.
- Headers: `Authorization: Bearer <OPENAI_API_KEY>`. Base URL default `https://api.openai.com/v1`.

Code Changes:
- `src/OrchestratorChat.Saturn/Models/SaturnModels.cs`
  - Add `ProviderType.OpenAI`.
- `src/OrchestratorChat.Saturn/Core/SaturnCore.cs`
  - Handle `ProviderType.OpenAI` in provider creation and available providers list.
- `src/OrchestratorChat.Saturn/Providers/ProviderFactory.cs`
  - Recognize `"openai"` in `CreateProviderAsync` and `CreateClient`.
  - Add `CreateOpenAIClient` using `OPENAI_API_KEY`.
- `src/OrchestratorChat.Saturn/Providers/OpenAI/*`
  - `OpenAIOptions.cs`: basic options (API key, base URL, default model).
  - `OpenAIClient.cs`: minimal client with streaming and non‑streaming chat completions.
  - `OpenAIProvider.cs`: Saturn provider implementing `LLMProviderBase` with streaming.

Configuration:
- Env var: `OPENAI_API_KEY` (preferred); can also be passed via Saturn provider settings.
- Optional setting: `BaseUrl` under `ProviderConfiguration.Settings`.

Usage (example):
- Create provider via `ISaturnCore.CreateProviderAsync(ProviderType.OpenAI, new Dictionary<string, object> { ["ApiKey"] = "..." })`.
- Or rely on `OPENAI_API_KEY` and pick models like `gpt-5`.

Follow‑ups / Next Steps:
- Implement non‑streaming `GetCompletionAsync`.
- Add tool/function call support for OpenAI parity with OpenRouter.
- Surface provider validation status in UI and setup wizard.
- Extend model list by querying `/models` (and cache).

Notes on “OpenAI Plus”:
- Codex CLI uses the official OpenAI API, not the consumer “ChatGPT Plus” web session. This implementation does the same for reliability and compliance. If web session integration is ever desired, it should live as a separate provider with explicit guardrails.


