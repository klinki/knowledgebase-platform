# Agent Guidelines for Sentinel Knowledgebase Project

This document provides guidelines for AI assistants working on the Sentinel Knowledgebase project.

## Deep-Dive Skills (Progressive Disclosure)
Detailed coding standards, conventions, and workflows are isolated into specific skill files. When working on a task, you **MUST** consult the relevant guidelines below if you are uncertain about the project's patterns:

### Coding Standards
- [C# and .NET Conventions](.agents/docs/csharp-conventions.md)
- [Testing Patterns](.agents/docs/testing-patterns.md)
- [Git Workflow and Commits](.agents/docs/git-workflow.md)

### Agent Workflows & Protocols
- [Communication Preferences](.agents/docs/communication-guidelines.md)
- [Docs-as-Code Workflow](.agents/docs/docs-as-code-workflow.md)
- [Code Review Protocol](.agents/docs/code-review-protocol.md)


## Code Style Guidelines

### TypeScript
- Use strict mode
- Prefer `const` and `let` over `var`
- Use explicit return types on exported functions
- Prefix unused parameters with underscore (e.g., `_sender`)

### File Organization
- Source files in `src/` directory
- Compiled output in `dist/` directory (gitignored)
- One main class/component per file

### Naming Conventions
- Files: kebab-case (e.g., `content-script.ts`)
- Classes: PascalCase
- Functions/variables: camelCase
- Constants: UPPER_SNAKE_CASE

## Communication Preferences

### Before Making Changes
1. Confirm understanding of requirements
2. Propose approach if not explicitly specified
3. Ask for clarification on ambiguous requirements

### After Completing Tasks
1. Summarize what was done
2. List files created/modified
3. Note any follow-up actions needed
4. Update todo list if applicable

### When Asking Questions
- Provide 2-4 specific, actionable options
- Prioritize options by logical sequence
- Include mode switches when appropriate

## Technology Stack

### Browser Extension
- Vanilla TypeScript (no framework)
- Chrome Manifest V3
- ES2024 target
- ESNext modules with bundler resolution

### Backend (Future Phases)
- Node.js with Fastify/Express
- PostgreSQL with pgvector
- OpenAI API for LLM processing

## Project Phases Reference

1. **Phase 1**: Browser Extension (The Collector) ✓
2. **Phase 2**: Ingestion Engine (API & Pipeline)
3. **Phase 3**: The Vault (Storage & Retrieval)
4. **Phase 4**: Discovery Dashboard (UI)

## Important Notes

- Never commit `node_modules/` or `dist/` directories
- Always run `npm run build` before committing TypeScript changes
- Test extension in Chrome developer mode before marking complete
- Update this document if new conventions are established

## Testing Commands

### Browser Extension Tests

Run from the `browser-extension` directory:

```bash
# Unit and integration tests
npm run test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage report
npm run test:coverage

# Run E2E tests with Playwright
npm run test:e2e

# Run E2E tests with UI
npm run test:e2e:ui

# Run all tests (unit + E2E)
npm run test:all
```

### CI/CD

Tests are automatically run in GitHub Actions:
- Unit and integration tests run on every push/PR
- E2E tests run on every push/PR
- Coverage reports are uploaded as artifacts
