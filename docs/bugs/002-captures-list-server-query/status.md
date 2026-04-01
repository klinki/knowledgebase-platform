# Bug Status

## Current State
- awaiting_user_confirmation

## Active Attempt
- `fix-attempt-001.md`

## Last Updated
- 2026-03-31

## Confirmation Date
- pending

## Resolution Summary
- Capture list sorting, filtering, and pagination now run through a new backend paged list endpoint, and the frontend requests only the current page instead of processing the full dataset locally.

## Attempt History
- `fix-attempt-001.md` - created

## State Change Log
- 2026-03-31: bug opened
- 2026-03-31: investigation completed and first fix attempt started
- 2026-03-31: backend paged list contract implemented and frontend state moved to server-driven queries
- 2026-03-31: unit tests, integration tests, frontend tests, and builds passed; awaiting user confirmation

## Notes
- Goal is to move captures-page sorting, filtering, and pagination to the backend without changing the visible UI flow.
