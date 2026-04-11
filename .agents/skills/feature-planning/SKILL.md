---
name: Feature Planning and Scaffolding
description: Creates or saves feature specs and implementation plans, automatically persists the last approved planning artifact before execution, and scaffolds feature documentation for delivery.
triggers:
  - "plan a new feature"
  - "create feature plan"
  - "create implementation plan"
  - "create feature spec"
  - "plan implementation"
  - "scaffold feature"
  - "start a new epic"
---

# Execution Protocol: Feature Planning

When triggered, translate the user's request into one or both of these artifacts:

- `feature-spec.md` for product definition
- `implementation-plan.md` for technical execution

Do not mix them by default. Create both only when both are actually needed.

## Artifact Selection

Choose the artifact type before drafting.

### Use `feature-spec.md` when

- the problem, goals, scope, UX, or acceptance criteria are still being defined
- the user is asking what should be built
- multiple product directions are still plausible

### Use `implementation-plan.md` when

- the feature is already understood well enough to build
- the user is asking how to implement it
- the next step is engineering execution rather than product definition

### Use both when

- the user wants to define the feature and then plan implementation
- requirements are not stable enough for execution yet, but implementation planning will follow

When both are needed, produce them sequentially:

1. `feature-spec.md`
2. `implementation-plan.md`

## Plan Mode Compatibility

This skill must behave differently depending on the active collaboration mode.

### If active mode is Plan Mode

- Do not create or edit files.
- Do not scaffold feature folders.
- Gather context, clarify intent, select the correct artifact type, and produce a final `<proposed_plan>` only.
- The final artifact must include:
  - the chosen feature slug
  - the exact target documentation path
  - the artifact type: `feature-spec.md` or `implementation-plan.md`
- If the user asks for implementation planning while the feature is still underspecified, produce a `feature-spec.md` artifact first.
- If the user says "save the plan" while still in Plan Mode, do not regenerate or rephrase it.
- Treat the approved `<proposed_plan>` as the canonical content to be saved verbatim once Plan Mode ends.

### If active mode is Default mode

- If there is an approved `<proposed_plan>` from the immediately preceding planning turn, treat it as the source of truth.
- When the user asks to implement after planning, automatically persist the last approved artifact before starting implementation.
- Do not wait for the user to separately ask to save the plan/spec.
- When the user asks to save, scaffold, or implement:
  1. Create the target feature folder if needed.
  2. Save the last approved `<proposed_plan>` verbatim to the target file.
  3. Create supporting folders such as `reviews/` if needed.
  4. If the user also asked for implementation, begin implementation after saving the approved artifact.

## Verbatim Persistence Rule

When saving an approved artifact generated in Plan Mode:

- do not rewrite it
- do not generate a replacement artifact
- do not add execution steps into the saved document unless they were already part of the approved `<proposed_plan>`
- preserve headings, bullets, wording, and file path exactly

## Step 1: Information Gathering

If the user's prompt is too brief, gather enough context to determine:

1. Whether the user needs a spec, an implementation plan, or both.
2. The primary goal of the feature.
3. The core acceptance criteria.
4. Any known architectural constraints or affected data models.

## Step 2: Workspace Determination

Once you have enough context, determine the canonical feature path and file naming:

1. Determine a concise, hyphenated name for the feature (for example, `03-shopping-cart`).
2. Choose the canonical documentation path under `/docs/features/[feature-name]/`.
3. Use `/docs/features/[feature-name]/feature-spec.md` for product-definition artifacts.
4. Use `/docs/features/[feature-name]/implementation-plan.md` for technical execution artifacts.

Only scaffold directories and files in Default mode.

## Step 3: Document Generation

Generate a decision-complete artifact that can be saved without rewording.

Use:

- `docs/templates/feature-spec.md` for `feature-spec.md`
- `docs/templates/implementation-plan.md` for `implementation-plan.md`

In Plan Mode:

- present the approved artifact as `<proposed_plan>`
- include the exact save path inside the artifact
- do not scaffold or write the file

In Default mode:

- if there is an approved artifact from the immediately preceding planning turn, save that artifact verbatim
- do not regenerate it unless the user explicitly asked for revisions

## Step 4: Update Global Hub

After the feature folder and canonical artifact file are created in Default mode:

1. Check whether `/docs/STATUS.md` exists.
2. If it exists, add the new feature to the `### Current Milestones` or `### Backlog` list as `[ ] [Feature Name] (Ref: /docs/features/[feature-name]/[canonical-file-name])`.
3. If it does not exist, skip the global hub update silently.
4. If the user also asked for implementation, continue directly into execution instead of waiting for another approval.
5. If the user asked only for planning/scaffolding, inform them that the artifact has been saved and is ready for the next phase.
