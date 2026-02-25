---
description: Docs-as-Code workflow and architectural rules for Inviser.
---

# Docs-as-Code Workflow

You are an expert software engineer operating in a Docs-as-Code environment. Adhere strictly to the following workflow 
rules to maintain project synchronization. For each action, you will find a template in the `/docs/templates` directory.

## 1. Context Gathering (Before Coding)
Whenever you are given a new task or feature request:
- DO NOT start writing code immediately.
- FIRST, read `/docs/STATUS.md` to understand the high-level project state.
- SECOND, read `/docs/ARCHITECTURE.md` and any relevant ADRs in `/docs/adrs/` to ensure your proposed solution adheres 
  to project constraints.
- THIRD, locate the specific feature file in `/docs/features/` (e.g., `01-user-auth.md`). Read its Implementation Status 
  and Acceptance Criteria.

## 2. Execution & Testing
- Base all implementation strictly on the Acceptance Criteria found in the feature file.
- Practice Test-Driven Development (TDD): write the test for the acceptance criterion first, then write 
  the implementation to make it pass.

## 3. State Management (After Coding / Before Finishing)
Before concluding your response or finalizing a commit:
- You MUST update the feature markdown file in `/docs/features/`.
- Change the status of the completed task from `[ ]` or `[-]` to `[x]`.
- If you discovered new necessary sub-tasks during implementation, append them to the "Implementation Status" list as `[ ]`.
- Do not update the global `/docs/STATUS.md` unless an entire feature file is 100% complete.

## 4. Architectural Boundaries
- Never introduce a new database, state management library, or core architectural pattern without first prompting the
  user to create a new ADR.
