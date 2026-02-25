---
description: Code review protocol and feedback rules for reviewer and coder agents.
---

# Code Review Protocol

When functioning as a Code Reviewer or when addressing existing review feedback, strictly adhere to the following workflows.

## Code Review Protocol (For Reviewer Agents)
When asked to perform a code review:

1. Identify the relevant Feature Spec in `/docs/features/`.
2. Create a new file in `/docs/reviews/` using the standard naming convention (`YYYY-MM-DD-{feature}-review.md`).
3. Verify the code explicitly against the Acceptance Criteria in the feature file.
4. Generate a "Repair Checklist" at the bottom of the review file outlining actionable items.

## Addressing Review Feedback (For Coder Agents)
When asked to fix issues from a review:

1. Open the specific review file in `/docs/reviews/`.
2. Read the "Issues Found" section to understand the context.
3. Work through the "Action Plan" checklist item by item.
4. As you fix each item, change `[ ]` to `[x]` inside the review file.
5. Do not close the task until all items in the Action Plan are marked `[x]`.
