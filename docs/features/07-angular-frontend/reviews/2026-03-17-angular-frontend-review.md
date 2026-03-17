# Angular Frontend User Experience Review

Date: 2026-03-17

## Scope

User-perspective review of the authenticated Angular application, focused on
navigation clarity, capture workflows, and whether the UI supports normal
end-to-end usage without operator knowledge.

## Findings

### High

1. Dashboard search and recent capture items are not actionable.
   - File: `frontend/src/app/features/dashboard/dashboard.component.ts`
   - Detail: the main list renders each result as a static `.knowledge-item`
     container with no link, click handler, or keyboard action.
   - Impact: users can search, see something relevant, and then hit a dead end.
     The dashboard behaves like a report, not a working entry point into the
     knowledge base.

### Medium

2. Manual captures show a broken empty source link in capture detail.
   - File: `frontend/src/app/features/captures/capture-detail.component.ts`
   - Detail: the overview always renders `sourceUrl` as an anchor, even when the
     value is empty for direct-content captures.
   - Impact: manually created captures look incomplete or broken, and the UI
     does not distinguish “manual capture” from “missing data”.

3. The first-run dashboard does not guide users to their next useful action.
   - File: `frontend/src/app/features/dashboard/dashboard.component.ts`
   - Detail: the empty state only says no captures exist yet. It does not link
     to create a capture, connect the extension, or invite teammates.
   - Impact: a newly signed-in user can land on a visually polished page and
     still not know what to do next.

4. Capture failure status is visible, but failure reason and recovery are not.
   - Files:
     - `frontend/src/app/features/captures/captures.component.ts`
     - `frontend/src/app/features/captures/capture-detail.component.ts`
   - Detail: users can see `Failed`, but there is no surfaced failure reason,
     retry action, or help text.
   - Impact: the product currently assumes server access or log access when
     capture processing fails, which breaks the normal frontend user experience.

## What Works

- Logged-in navigation is coherent and now covers dashboard, captures, tags,
  invitations, and manual capture creation.
- The captures list and detail pages make the ingestion pipeline legible.
- The invitation flow is understandable and fits the current admin model.
- The direct capture page is simple enough for normal use and does not overload
  the user with unnecessary options.

## Repair Checklist

- [ ] Make dashboard items navigable to capture detail or other relevant detail pages.
- [ ] Replace empty `sourceUrl` links in capture detail with a manual-capture label or non-link value.
- [ ] Add first-run CTAs on the empty dashboard state for `Create Capture`,
      extension onboarding, and optionally invitations.
- [ ] Expose capture failure reason and a retry path in the frontend when a
      capture has status `Failed`.
