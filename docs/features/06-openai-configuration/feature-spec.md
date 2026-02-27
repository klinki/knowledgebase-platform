# Feature Specification: OpenAI Configuration

## Goal
Move hardcoded OpenAI API URLs from the application code into the configuration system (`appsettings.json` and `.env.example`) to improve maintainability and support different enviroments.

## Scope
- **Configuration Defaults**: Add default OpenAI URLs to `appsettings.json`.
- **Environment Support**: Add environment variable placeholders to `.env.example`.
- **Code Refactoring**: Update `ContentProcessor.cs` to load these URLs from `IConfiguration`.

## Acceptance Criteria
- [ ] OpenAI Embedding and Chat Completion URLs are no longer hardcoded in `ContentProcessor.cs`.
- [ ] URLs are loaded from `OpenAI:EmbeddingsUrl` and `OpenAI:ChatCompletionsUrl`.
- [ ] Default values are provided in `appsettings.json`.
- [ ] `.env.example` contains placeholders for these URLs.
- [ ] Application continues to function correctly with existing (default) URLs.

## Implementation Status
### Phase 1: Planning & Setup
- [x] [DONE] Create feature specification.
- [ ] Create implementation plan.

### Phase 2: Implementation
- [x] [DONE] Update `appsettings.json` with OpenAI URL defaults.
- [x] [DONE] Update `.env.example` with URL environment variables.
- [x] [DONE] Refactor `ContentProcessor.cs` to use configuration.

### Phase 3: Verification
- [x] [DONE] Verify unit tests for `ContentProcessor`.
- [x] [DONE] Verify application startup.

## Verification Plan
- [ ] **Unit Tests**: Run `ContentProcessorTests.cs`.
- [ ] **Manual Check**: Check logs/output to ensure no configuration errors on startup.
