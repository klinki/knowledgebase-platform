# Frontend Authentication Plan

## Summary

Harden the Angular dashboard authentication flow so session state is
centralized, cookie-backed, and consistent across guards, shell rendering, and
API failures.

This document covers only frontend authentication and session behavior. Dashboard
data state, tags state, and backend read models for those views are planned
separately under feature `11-frontend-dashboard`.

## Key Frontend Auth Changes

- Keep a single Angular session source of truth in the auth service.
- Model auth lifecycle explicitly as `unknown`, `authenticated`, or
  `anonymous`.
- Resolve the initial session via `GET /api/auth/me` before protected shell
  content renders.
- Add an HTTP interceptor that applies `withCredentials: true` to API requests
  and centralizes `401 Unauthorized` handling.
- Redirect expired or missing sessions to
  `/login?returnUrl=<current-route>`.
- Update the route guard to wait for session resolution and preserve the
  attempted destination.
- Keep login and logout flows aligned with server-backed cookie sessions.
- Preserve browser extension device approval behavior when the login page is
  opened with a `userCode`.

## Public Interfaces and Behavior

### Auth state model

- `unknown`: app startup has not finished resolving the current session yet.
- `authenticated`: current user is loaded and protected routes may render.
- `anonymous`: no valid session is present and protected routes must redirect to
  login.

### Frontend behaviors

- Initial app bootstrap performs a session check before protected layout content
  is shown.
- Successful login redirects to the original `returnUrl` when present, otherwise
  to the default dashboard route.
- Logout clears client auth state and returns the user to the login screen.
- Any protected API response returning `401 Unauthorized` clears auth state once
  and redirects to login.
- Device approval from the extension remains supported through the existing
  `userCode` query parameter flow.

### Angular implementation direction

- Keep the current signals-based approach; do not introduce a store library.
- Add a shared auth interceptor under `core`.
- Remove per-request `withCredentials` duplication from individual services.
- Prevent route-guard and shell-render races during first-load session restore.

## Test Plan

- Valid existing cookie session restores the authenticated shell on page load.
- Anonymous access to protected routes redirects to login with a `returnUrl`.
- Successful login redirects to the original route when `returnUrl` is present.
- Logout clears session state and returns the user to `/login`.
- A `401 Unauthorized` response from a protected API request clears auth state
  and redirects only once.
- Device approval still works when login and approval happen in the same page
  flow.
- Playwright coverage verifies login, guard redirects, session restore, and
  auth-expiry redirect behavior.

## Assumptions and Defaults

- Cookie-based API sessions remain the dashboard authentication mechanism.
- The frontend should not store dashboard auth tokens locally.
- Redirect-on-`401` is the desired behavior for expired, revoked, or missing
  sessions.
- Admin invitation, password reset, and user-management screens are out of scope
  for this document.
