# Browser Extension User Experience Review

Date: 2026-03-17

## Scope

User-perspective review of the browser extension, focused on setup clarity,
capture discoverability, and whether the extension communicates its real
capabilities accurately.

## Findings

### High

1. The popup misrepresents the extension as X-only even though it also supports
   bookmark capture and page selection capture.
   - File: `browser-extension/src/popup.ts`
   - Detail: popup status switches between `Active on X.com` and
     `Navigate to X.com to use`.
   - Impact: users on normal pages are told the extension is unusable, even
     though they can still save selections and bookmarked pages. This makes the
     extension feel narrower and more confusing than it actually is.

### Medium

2. The popup’s “View Dashboard” action is tied to the stored API URL rather than
   an explicit app/dashboard URL.
   - File: `browser-extension/src/popup.ts`
   - Detail: it opens `apiUrl` directly.
   - Impact: this only works cleanly when API and frontend share one origin. In
     local or split-host setups, users can land on the wrong destination.

3. The extension still tells users to “Check API key in settings” when capture
   fails, even though browser sign-in is now the primary authentication flow.
   - File: `browser-extension/src/content.ts`
   - Detail: the tweet save error tooltip references API key setup.
   - Impact: error recovery guidance is outdated and sends users toward a legacy
     path instead of the supported sign-in flow.

4. The popup documentation link is still a placeholder.
   - File: `browser-extension/src/popup.ts`
   - Detail: it opens `https://github.com/yourusername/sentinel-knowledgebase`.
   - Impact: a user asking for help is sent to a broken or irrelevant
     destination, which undermines trust quickly.

## What Works

- The device sign-in flow in settings matches the backend model and is practical
  for a browser extension.
- Background capture coverage is strong: tweets, bookmarked pages, and selected
  text all map into the same backend contract.
- The settings page exposes the few controls a user actually needs today.

## Repair Checklist

- [ ] Replace X-only popup status with capability-aware messaging that covers X,
      page selection, bookmark capture, and sign-in state.
- [ ] Separate dashboard/app URL from API URL in extension settings and popup behavior.
- [ ] Update extension error copy to point users to browser sign-in first and
      legacy token use only as fallback.
- [ ] Replace the placeholder documentation link with a real project help destination.
