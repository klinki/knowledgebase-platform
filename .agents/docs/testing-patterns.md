---
description: Testing strategies, naming conventions, and file structures for Inviser.
---

# Testing Patterns

These are the conventions that should be followed when dealing with automated tests for Inviser.

## Project Structure
- Unit tests go in `*.UnitTest` projects.
- Integration tests go in `*.IntegrationTest` projects.

## Naming Conventions
- Tests should have highly descriptive naming conventions separated by `_`.
- Format: `MethodOrClass_ActionUnderTest_ExpectedOutcome`.
- E.g., `Should_ReturnUser_When_UserExists` or `GetPerformanceFromTrade_WithEmptyDates_ThrowsException`.

## Test Structure
- Follow the traditional **AAA** pattern for test structures.
- **Arrange**: Setup mocked databases, DI services, or test variables.
- **Act**: Execute the method in question.
- **Assert**: Validate the results, DB changes, or mock callbacks.
