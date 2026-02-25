---
name: Feature Planning and Scaffolding
description: Creates a new feature folder, establishes the feature specification (PRD), and generates the mandatory TODO list for execution.
triggers:
  - "plan a new feature"
  - "create feature spec"
  - "scaffold feature"
  - "start a new epic"
---

# Execution Protocol: Feature Planning

When triggered, you act as a Technical Product Manager. Your goal is to translate the user's feature request into a structured, 
agent-actionable workspace. Do not write implementation code during this phase.

## Step 1: Information Gathering

If the user's prompt is too brief, ask clarifying questions to determine:

1. The primary goal of the feature.
2. The core acceptance criteria.
3. Any known architectural constraints or database models involved.

## Step 2: Workspace Creation

Once you have enough context, scaffold the feature workspace:

1. Determine a concise, hyphenated name for the feature (e.g., `03-shopping-cart`).
2. Create the feature directory: `/docs/features/[feature-name]/`.
3. Create the reviews sub-directory: `/docs/features/[feature-name]/reviews/`.
4. Create the main specification file: `/docs/features/[feature-name]/feature-spec.md`.

## Step 3: Document Generation

Populate the `feature-spec.md` file using the exact template below. You must generate a logical, 
granular TODO list under "Implementation Status".

Use a template from `docs/templates/feature-spec.md`


## Step 4: Update Global Hub

After the feature folder and spec are created:
1. Open `/docs/STATUS.md`.
2. Add the new feature to the `### Current Milestones` or `### Backlog` list as `[ ] [Feature Name] (Ref: /docs/features/[feature-name]/feature-spec.md)`.
3. Inform the user that the planning phase is complete and ask if they would like you to begin executing the first TODO item.
