# Code Review: [Feature Name]

**Date:** [YYYY-MM-DD]
**Reviewer:** [Agent Name / Human Name]
**Target Branch/Commit:** [Commit Hash or Branch Name]
**Associated Feature Spec:** `/docs/features/[feature-file].md`

## 1. Specification Compliance Check
*Comparing implementation against Acceptance Criteria in the Feature Spec.*

- [ ] **Criteria 1:** [Pass/Fail] - [Observation]
- [ ] **Criteria 2:** [Pass/Fail] - [Observation]
- [ ] **Tests:** [Are tests present and passing? Yes/No]

## 2. Issues Found
*Critical bugs, logic errors, or security flaws.*

### [Severity: High/Medium/Low] - [Short Description]
- **File:** `path/to/file.ts`
- **Context:** [Describe why this is wrong. E.g., "The password is being logged in plain text."]
- **Suggestion:** [How to fix it.]

## 3. Action Plan (Repair Checklist)
*The Coder Agent must check these off as they are fixed.*

- [ ] Fix critical security issue in `auth.service.ts`.
- [ ] Add missing test case for "expired token".
- [ ] Refactor the loop in `utils.ts` to improve O(n) performance.

## 4. Final Verdict
- [ ] **APPROVE:** Ready to merge.
- [ ] **REQUEST CHANGES:** Fix issues in "Action Plan" and re-submit.
