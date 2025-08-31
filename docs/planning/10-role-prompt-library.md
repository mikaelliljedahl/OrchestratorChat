# Role Prompt Library & Configuration

## Purpose
Provide a configurable library of role prompts (orchestrator/lead, planner, coder, researcher, reviewer) used at initialization and restart loops.

## Goals
- Centralize standard prompts per role
- Allow updates without code changes (config or files)
- Provide default templates and environment overrides

## Design
- PromptLibraryService
  - Loads role prompts from configuration or JSON files at startup; supports hot reload (optional)
  - API: GetPrompt(role), GetTemplate(role, key), ListRoles()
- Storage
  - Option A: appsettings.json with nested sections
  - Option B: JSON files under 'prompts/roles/*.json' (deployed with app)
  - Support environment-specific overrides: prompts/roles.{Environment}/*.json
- Integration
  - Orchestrator and Agents request prompts by role on init/restart
  - Supervisor (08) restarts lead agent using lead/orchestrator prompt
  - Context summarization uses a summarizer prompt (09)

## Configuration Example
{
  'Prompts': {
    'Roles': {
      'Lead': { 'System': 'You are the lead orchestrator…' },
      'Planner': { 'System': 'You decompose goals…' },
      'Coder': { 'System': 'You write clean, tested code…' },
      'Researcher': { 'System': 'You investigate APIs…' },
      'Reviewer': { 'System': 'You verify acceptance criteria…' }
    },
    'Templates': {
      'Summarizer': 'Summarize prior messages into key facts & decisions…'
    }
  }
}

## Acceptance Criteria
- Role prompts load at startup and can be fetched by role
- Restarting an agent uses the correct role prompt
- Summarization prompt can be fetched and used by ContextManager

## Tests
- Unit: PromptLibraryService loads from config/files; supports overrides
- Integration: Orchestrator uses PromptLibrary to initialize/restart agents; summarization pulls template

---

## Next Steps (PR Slices)
1) Implement PromptLibraryService + config schema; DI
2) Add default prompts JSON; wire to Orchestrator/Supervisor/ContextManager
3) Admin surface (later): view/edit prompts; hot reload
