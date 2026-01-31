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
