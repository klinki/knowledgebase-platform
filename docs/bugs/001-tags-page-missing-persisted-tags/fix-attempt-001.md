# Fix Attempt 001

## Goal
Make the tags page show persisted tags after refresh, including tags that currently have zero captures.

## Proposed Change
- Add a tag-list service path that returns all tags for the current user, not only tags with `Count > 0`.
- Keep dashboard tag ranking behavior unchanged.
- Add an integration test that creates an unused tag and verifies `GET /api/v1/tags` returns it with `Count = 0`.

## Risks
- Accidentally broadening dashboard tag lists if the same repository query is reused everywhere.
- Changing the API contract in a way that affects tag ranking or stats.

## Expected Verification
- `backend/tests/SentinelKnowledgebase.IntegrationTests/TagsControllerTests.cs`
- Focused backend integration test run for the tags controller

## Files or Components Involved
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/IRepositories.cs`
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/TagRepository.cs`
- `backend/src/SentinelKnowledgebase.Application/Services/Interfaces/ITagService.cs`
- `backend/src/SentinelKnowledgebase.Application/Services/TagService.cs`
- `backend/src/SentinelKnowledgebase.Api/Controllers/TagsController.cs`
- `backend/tests/SentinelKnowledgebase.IntegrationTests/TagsControllerTests.cs`

## Outcome
- Implemented a dedicated "all tags" path for the tags page while leaving dashboard tag ranking logic untouched.
- The tags API now returns zero-count tags, so newly created persisted tags remain visible after refresh.
- Added integration coverage for the unused-tag case.
- Verification: `dotnet test backend/tests/SentinelKnowledgebase.IntegrationTests/SentinelKnowledgebase.IntegrationTests.csproj --filter TagsControllerTests`
- Result: 14 integration tests passed.
- Remaining gap: user confirmation in the browser frontend.
