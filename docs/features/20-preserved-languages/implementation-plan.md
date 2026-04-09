# Implementation Plan: Preserved Languages

- Feature slug: `20-preserved-languages`
- Artifact type: `implementation-plan.md`
- Save path: `docs/features/20-preserved-languages/implementation-plan.md`

## Summary

Add per-user language preferences so each user has:
- one default language
- a list of preserved languages

Capture processing will keep insights in the original language when the source language is preserved, unknown, or already matches the user's default language. Otherwise, AI-generated insight fields will be produced in the user's default language, and embeddings will continue to be generated from the stored summary text in that chosen output language. This applies only to newly processed captures.

## Key Changes

### User preferences and auth surface

- Extend `ApplicationUser` with `DefaultLanguageCode` using canonical lowercase base-language codes such as `en`, `de`, `cs`.
- Add a new `UserPreservedLanguage` persistence model keyed by `(UserId, LanguageCode)` with a uniqueness constraint.
- Extend `AuthUserDto` and frontend `User` state to include:
  - `defaultLanguageCode`
  - `preservedLanguageCodes`
- Add authenticated preferences endpoints under `api/auth`:
  - `GET /api/auth/preferences`
  - `PUT /api/auth/preferences`
- On first authenticated session for a user with no default language yet, seed `DefaultLanguageCode` from the request `Accept-Language` header using the first valid base language; if parsing fails, fall back to `en`.

### Processing policy

- Determine source language only from existing capture metadata language input; do not add AI language detection in v1.
- Normalize both user preferences and source language to base-language codes before comparison, so `en` matches `en-US` and `en-GB`.
- Change content processing so insight extraction accepts an optional output language code:
  - preserved source language: generate insight in source language
  - unknown source language: preserve original language
  - source equals default language: generate insight in that language without translation behavior
  - all other known languages: generate insight in the user's default language
- Keep `SourceTitle` and `Author` in original/source form; translate only generated insight fields:
  - `Title`
  - `Summary`
  - `KeyInsights`
  - `ActionItems`
- Keep embedding generation API unchanged, but continue generating embeddings from the stored summary text after the language decision has been applied.
- Leave semantic-search query handling unchanged in v1; rely on the existing multilingual embedding model rather than adding query fan-out or query translation.

### Frontend settings

- Add a new authenticated `/settings` page and sidebar entry for language preferences.
- The page should allow:
  - viewing the current default language
  - editing the default language
  - adding/removing preserved languages
- Use a fixed supported-language picker based on a shared frontend/backend canonical list of base-language codes plus display names.
- Saving preferences should update backend state and refresh the in-memory authenticated user state immediately.
- Do not require a separate onboarding flow; the seeded default language is editable from Settings.

## Public Interfaces

- `ApplicationUser`
  - add `DefaultLanguageCode`
- New persistence model
  - `UserPreservedLanguage { UserId, LanguageCode, CreatedAt }`
- `AuthUserDto`
  - add `DefaultLanguageCode`
  - add `PreservedLanguageCodes`
- New DTOs for preferences read/write
  - `UserLanguagePreferencesDto`
  - `UpdateUserLanguagePreferencesRequestDto`
- `IContentProcessor`
  - update `ExtractInsightsAsync` to accept target/output language context
- New authenticated API endpoints
  - `GET /api/auth/preferences`
  - `PUT /api/auth/preferences`

## Test Plan

- Backend integration tests:
  - first authenticated session seeds default language from `Accept-Language`
  - `GET /api/auth/me` and `GET /api/auth/preferences` include language settings
  - `PUT /api/auth/preferences` persists normalized base-language codes, de-duplicates values, and rejects invalid codes
- Processing tests:
  - known non-preserved source language produces generated insight fields in the user's default language
  - preserved source language keeps generated insight fields in the source language
  - unknown source language preserves original language
  - source language equal to default language does not trigger translation behavior
  - embedding generation uses the post-decision stored summary text
- Frontend tests:
  - `/settings` route is available inside the authenticated shell
  - settings form loads current values, saves changes, and updates auth state
  - preserved-language matching examples such as `en-US` against preserved `en` behave as expected in the displayed state
- No backfill tests are required because existing processed captures remain unchanged.

## Assumptions and Defaults

- Scope is new captures only; no automatic reprocessing or migration of existing `ProcessedInsight` rows.
- Unknown source language always preserves the original language.
- Browser locale seeding happens only when `DefaultLanguageCode` is empty; later user edits always win.
- Preserved-language matching uses canonical base-language codes, not exact locales.
- Redundant preservation of the default language can be removed on save because it has no behavioral effect.
- Existing human-readable `Language` labels remain as-is; this feature does not redesign label storage.
