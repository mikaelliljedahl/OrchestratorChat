# Context Window Management & Summarization

## Purpose
Avoid context window exhaustion across long-running, parallel agents by trimming history, summarizing, and refreshing context safely.

## Goals
- Detect nearing token limits (using Usage metadata or heuristics)
- Summarize/prune older history while preserving key facts/decisions
- Maintain a rolling 'working context' per agent/session

## Non-Goals
- External vector DB integrations (future)

## Design
- ContextManager (per session/agent)
  - Tracks message count, total chars/tokens, and recent tool outputs
  - Policies: hard cap (provider MaxTokens * factor), soft cap (start summarization at threshold)
- Summarization flow
  - When soft cap exceeded:
    - Summarize older messages -> 'ContextSummary Vn' (system message)
    - Prune raw messages replaced by the summary; keep decisions/tool results verbatim
  - When hard cap risk remains:
    - Create 'Working Set' with last N messages + latest summary + plan snapshot
- Provider integration
  - Use Usage tokens when available (ClaudeAgent maps Usage; Saturn may require heuristics)
  - Fallback heuristic: char count, message count, recent tool payload size

## Persistence
- SessionSnapshots can include ContextSummary blocks
- New entity (optional): ContextSummary { id, sessionId, agentId, version, content, createdAt }

## Configuration
- appsettings: Context: { SoftCapRatio, HardCapRatio, SummaryPromptKey, MaxWorkingMessages }
- Tie SummaryPromptKey to the prompt library (see next doc)

## Acceptance Criteria
- Long chat generates periodic 'ContextSummary Vn' and keeps going without provider errors
- Summarization preserves decisions and tool outputs; no critical info loss

## Tests
- Unit: summarize/prune selection logic; 'working set' builder
- Integration: simulate long session; ensure no 'token limit' errors; context rolls forward

---

## Next Steps (PR Slices)
1) Add ContextManager service + DI; config knobs
2) Summarization prompt integration (from prompt library)
3) Hook into Orchestrator/Agent send paths; drop-in usage where Usage tokens available
4) Tests for soft/hard cap behavior
