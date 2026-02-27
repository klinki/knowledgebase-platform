# Implementation Plan - Configure OpenAI API URLs

I will move the hardcoded OpenAI API URLs from `ContentProcessor.cs` into the application configuration, allowing for easier environment-specific overrides and better maintainability.

## Proposed Changes

### Configuration

#### [MODIFY] [appsettings.json](file:///c:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Api/appsettings.json)
Add default OpenAI URLs to the `OpenAI` section:
- `EmbeddingsUrl`: `https://api.openai.com/v1/embeddings`
- `ChatCompletionsUrl`: `https://api.openai.com/v1/chat/completions`

#### [MODIFY] [.env.example](file:///c:/ai-workspace/knowledgebase-platform/backend/.env.example)
Add placeholders for the new environment variables:
- `OPENAI_EMBEDDINGS_URL`
- `OPENAI_CHAT_COMPLETIONS_URL`

### Application Layer

#### [MODIFY] [ContentProcessor.cs](file:///c:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Application/Services/ContentProcessor.cs)
- Read `OpenAI:EmbeddingsUrl` and `OpenAI:ChatCompletionsUrl` from `IConfiguration`.
- Use these variables in `GenerateEmbeddingAsync` and `CallOpenAIForInsights`.
- Provide sensible defaults in code as a fallback.

## Verification Plan

### Automated Tests
- Run existing `ContentProcessorTests.cs` to ensure no regressions in fallback logic.

### Manual Verification
- Verify that the application starts correctly with the new configuration.
