# Feature: User Authentication (10-user-authentication)

## Goal

Add real authentication and authorization to Sentinel for the Angular dashboard, browser extension, and protected backend endpoints.

## Acceptance Criteria

- [x] Dashboard users can sign in with local Sentinel accounts managed through ASP.NET Core Identity.
- [x] Dashboard auth state is server-backed and survives page refresh via secure cookies.
- [x] Browser extension can authenticate without pasting a permanent API key.
- [x] Extension obtains short-lived access tokens and refresh tokens through a browser-assisted device login flow.
- [x] All non-public backend endpoints require authentication.
- [x] Role-based authorization exists for `admin` and `member`.
- [x] Admins can invite users, revoke sessions/tokens, and reset passwords.
- [x] Hangfire dashboard is not publicly exposed and requires authenticated admin access outside local development.
- [x] Existing anonymous health and local-development documentation surfaces remain explicitly scoped.

## Architecture Notes

- ASP.NET Core Identity is the source of truth for local users, password hashing, roles, and dashboard sign-in.
- Angular dashboard authentication uses secure cookie-based sessions backed by the API.
- Browser extension authentication uses a Sentinel-managed device authorization flow that issues short-lived bearer access tokens and refresh tokens.
- Detailed Angular auth and session hardening planning lives in `frontend-auth.md` for this feature.
- Initial authorization model is limited to `admin` and `member`.
- User creation is invite-only in v1, password recovery is admin reset only, and MFA is deferred but should remain design-compatible.
- External OIDC providers are out of scope for implementation in this feature but should remain a viable future migration path.
- `docs/templates/feature-spec.md` is currently missing; this feature spec follows the established repository pattern used by existing feature docs.

## Important Interfaces and Behavior

### Backend auth endpoints

- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/invitations`
- `POST /api/auth/users/{id}/reset-password`
- `POST /api/auth/device/start`
- `POST /api/auth/device/poll`
- `POST /api/auth/token/refresh`
- `POST /api/auth/token/revoke`

### Identity and authorization model

- Identity user for local accounts.
- Identity roles: `admin`, `member`.
- Cookie auth for the Angular app.
- Token auth for extension calls.
- Extension token claims include `userId`, `role`, `sessionId` or `deviceSessionId`, and scopes such as `capture:write`, `search:read`, and `offline_access`.

### Extension UX

- Primary flow opens Sentinel login and approval in the browser.
- After approval, the extension stores issued tokens in `chrome.storage.local`.
- Manual long-lived API keys are not the primary design target.

## Implementation Status

- [x] Add ASP.NET Core Identity to the API and persistence layer.
- [x] Define `admin` and `member` roles and seed/bootstrap the first admin safely.
- [x] Implement cookie-based dashboard auth endpoints: login, logout, current-user.
- [x] Replace frontend stub auth state with real session-backed auth integration.
- [x] Harden Angular auth state with centralized session resolution, credential interception, `returnUrl` handling, and `401` redirects.
- [x] Add invite-only user creation and admin password reset flow.
- [x] Add device authorization endpoints for browser extension sign-in.
- [x] Add access-token and refresh-token issuance, rotation, and revocation for extension sessions.
- [x] Update browser extension settings/auth UX to use web sign-in instead of manual API key as the primary path.
- [x] Protect capture, search, and admin endpoints with authentication and authorization policies.
- [x] Restrict Hangfire dashboard to admins.
- [x] Add integration and frontend/E2E coverage for login, authorization, token refresh, and revocation.
- [x] Document future OIDC migration considerations without implementing them in this feature.
- [x] Run backend integration suite in a Docker-enabled environment to execute the auth integration coverage end to end.

## Verification Plan

- [x] Frontend Playwright suite covers login, guard redirects, dashboard access, and mocked session-backed auth flows.
- [x] Frontend Playwright suite covers session restore, `returnUrl` redirects, and redirect-on-`401` behavior.
- [x] Browser extension Vitest suite covers authenticated capture requests and refresh-token-based access-token renewal.
- [x] .NET solution build and backend unit tests pass after the authentication changes.
- [x] Valid dashboard login sets an auth cookie and `GET /api/auth/me` returns the signed-in user.
- [x] Invalid dashboard login returns `401 Unauthorized`.
- [x] Anonymous requests to protected capture and search endpoints return `401 Unauthorized`.
- [ ] `member` access to admin-only endpoints returns `403 Forbidden`.
- [x] Invite flow creates a usable account without exposing public registration.
- [ ] Admin password reset invalidates prior sessions as designed.
- [x] Device flow remains pending until browser approval, then issues access and refresh tokens.
- [ ] Extension refresh token rotation works and old refresh tokens cannot be reused.
- [ ] Revoked extension tokens stop future authenticated API use.
- [x] Frontend route guards redirect anonymous users and restore authenticated state on reload.
- [ ] Hangfire dashboard is blocked for anonymous and non-admin users.
- [x] Execute the Docker-backed backend integration suite in an environment where Docker is available.

## Assumptions and Defaults

- Initial target is a small self-hosted team, not multi-tenant SaaS.
- Sentinel owns local accounts first; external OIDC is a future migration path.
- Public self-signup is out of scope.
- Email-based recovery is out of scope for v1.
- MFA is out of scope for v1 but should not be designed out.
- Production deployment uses HTTPS and same-site dashboard/API hosting where possible.
