# Tags Page Missing Persisted Tags

**Status:** Open

## Normalized Title
Tags page does not show persisted tags after refresh when they have no captures.

## Reported Symptoms
- The tags page shows tags immediately after adding them.
- After a refresh, those same tags disappear from the page.
- Existing tags that have not been attached to a capture are not visible.

## Expected Behavior
- The tags page should list all persisted tags for the signed-in user.
- Newly created tags should remain visible after a refresh.

## Actual Behavior
- The frontend only shows tags that already have usage data.
- Newly created tags appear in the current session because the create flow updates local state.
- A refresh reloads from the API, and zero-count tags are omitted.

## Reproduction Details
1. Open the frontend tags page.
2. Create a new tag without attaching it to a capture.
3. Observe that the tag appears in the current session.
4. Refresh the page.
5. Observe that the tag is no longer listed.

## Affected Area
- Frontend tags page
- Tags API contract consumed by the frontend

## Constraints
- Keep dashboard tag ranking behavior intact if possible.
- Avoid regressing rename and delete flows.

## Open Questions
- Should the dashboard continue to hide zero-count tags?
- Should the tags page show all tags or only tags with capture usage?
