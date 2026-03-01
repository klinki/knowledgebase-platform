---
name: Code Review Protocol
description: Workflow for producing feature-scoped code reviews and addressing review feedback with checklist-based tracking.
triggers:
  - "review this code"
  - "perform code review"
  - "fix review feedback"
  - "address review comments"
---

# Execution Protocol: Code Review

When functioning as a code reviewer, or when asked to address existing review feedback, use the workflows below.

## Reviewer Workflow

1. Identify the relevant feature specification in `/docs/features/`.
2. Create a new review file in `/docs/features/{feature}/reviews/` using:
   `YYYY-MM-DD-{feature}-review.md`
3. Verify implementation against the feature acceptance criteria.
4. Capture issues with severity and clear reproduction/impact details.
5. Add a "Repair Checklist" section with actionable checklist items.

## Coder Workflow (Addressing Review Feedback)

1. Open the specific review file in `/docs/features/{feature}/reviews/`.
2. Read the issues and assumptions sections first.
3. Execute the "Repair Checklist" item by item.
4. As each item is completed, change `[ ]` to `[x]` in the review file.
5. Do not close the task until all checklist items are marked `[x]`.

## Output Expectations

- Reviews should be specific, verifiable, and tied to acceptance criteria.
- Feedback should prioritize correctness and regression risk before style concerns.
- Repair checklist items should be testable and unambiguous.
