# Bug Status

## Current State
awaiting_user_confirmation

## Active Attempt
`fix-attempt-001.md`

## Last Updated
2026-04-12 - fix implemented and verified locally

## Confirmation Date
pending

## Resolution Summary
- Search results now include `captureId` in the combined search response, and the search page routes result cards to the capture detail page using that id.

## Attempt History
- `fix-attempt-001.md` - add capture id to combined search results and route search links with it

## State Change Log
- 2026-04-12: bug opened
- 2026-04-12: confirmed search result links use the processed insight id instead of the capture id
- 2026-04-12: investigation completed and first fix attempt started
- 2026-04-12: frontend and backend regression tests passed
- 2026-04-12: awaiting user confirmation

## Notes
- Keep the processed insight id available on the search response for compatibility.
