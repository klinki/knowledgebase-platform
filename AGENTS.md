# Agent Guidelines for Sentinel Knowledgebase Project

This document provides guidelines for AI assistants working on the Sentinel Knowledgebase project.

## Git Commit Workflow

### Commit Message Format

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

#### Types
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Changes that don't affect code meaning (formatting, semicolons, etc.)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Performance improvement
- `test`: Adding or correcting tests
- `chore`: Changes to build process, dependencies, etc.

#### Scopes
- `extension`: Browser extension code
- `api`: Backend API
- `db`: Database/schema
- `ui`: Dashboard/user interface
- `docs`: Documentation

### Required Attribution

**Every commit must include the co-author signature:**

```
Co-authored-by: Kilo Code <moonshotai/kimi-k2.5:free>
```

This must be in the footer of the commit message.

### Example Commit Messages

**Feature commit:**
```
feat(extension): add tweet capture functionality

Implement content script to detect and capture tweets from X.com.
Features include:
- MutationObserver for dynamic content detection
- DOM injection of save button
- Data extraction (ID, author, text, timestamp)

Co-authored-by: Kilo Code <moonshotai/kimi-k2.5:free>
```

**Chore commit:**
```
chore(extension): update TypeScript configuration

- Upgrade target to ES2024
- Enable stricter type checking
- Use bundler module resolution

Co-authored-by: Kilo Code <moonshotai/kimi-k2.5:free>
```

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

## Documentation

You are an expert software engineer operating in a Docs-as-Code environment.
Adhere strictly to the following workflow rules to maintain project synchronization.
For each action you will find template in `/docs/templates` directory.

## 1. Context Gathering (Before Coding)
Whenever you are given a new task or feature request:
- DO NOT start writing code immediately.
- FIRST, read `/docs/STATUS.md` to understand the high-level project state.
- SECOND, read `/docs/ARCHITECTURE.md` and any relevant ADRs in `/docs/adrs/` to ensure your proposed solution adheres to project constraints.
- THIRD, locate the specific feature file in `/docs/features/` (e.g., `01-user-auth.md`). Read its Implementation Status and Acceptance Criteria.

## 2. Execution & Testing
- Base all implementation strictly on the Acceptance Criteria found in the feature file.
- Practice Test-Driven Development (TDD): write the test for the acceptance criterion first, then write the implementation to make it pass.

## 3. State Management (After Coding / Before Finishing)
Before concluding your response or finalizing a commit:
- You MUST update the feature markdown file in `/docs/features/`.
- Change the status of the completed task from `[ ]` or `[-]` to `[x]`.
- If you discovered new necessary sub-tasks during implementation, append them to the "Implementation Status" list as `[ ]`.
- Do not update the global `/docs/STATUS.md` unless an entire feature file is 100% complete.

## 4. Architectural Boundaries
- Never introduce a new database, state management library, or core architectural pattern without first prompting the user to create a new ADR.

## 5. Code Review Protocol (For Reviewer Agents)
When asked to perform a code review:

1. Identify the relevant Feature Spec in `/docs/features/`.
2. Create a new file in `/docs/reviews/` using the standard naming convention (`YYYY-MM-DD-{feature}-review.md`).
3. Verify the code explicitly against the Acceptance Criteria in the feature file.
4. Generate a "Repair Checklist" at the bottom of the review file.

## 6. Addressing Review Feedback (For Coder Agents)
When asked to fix issues from a review:

1. Open the specific review file in `/docs/reviews/`.
2. Read the "Issues Found" section to understand the context.
3. Work through the "Action Plan" checklist item by item.
4. As you fix each item, change `[ ]` to `[x]` inside the review file.
5. Do not close the task until all items in the Action Plan are marked `[x]`.

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
