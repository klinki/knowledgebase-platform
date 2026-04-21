# Feature Spec: Messaging Ingestion v1 (Telegram First)

## Summary

- **Feature slug:** `26-messaging-ingestion-v1`
- **Artifact type:** `feature-spec.md`
- **Target doc path:** `docs/features/26-messaging-ingestion-v1/feature-spec.md`
- Add official **Telegram** ingestion first, using a shared bot and private-chat forwarding into the existing Sentinel capture pipeline.
- Defer **WhatsApp** and **Signal** from v1:
  - **Telegram** is the best fit now because it has an official bot surface and supports inbound updates through polling or webhooks.
  - **WhatsApp** is feasible later, but the official Cloud API is business-oriented and adds heavier account, phone-number, and webhook setup.
  - **Signal** is explicitly out of scope for official-only v1 because Signal does not provide an official bot-style integration surface; any practical automation would rely on unofficial tooling.
- Optimize for **official-only**, **Telegram private chats only**, **text and links only**, and **Telegram-first delivery** with reusable architecture for later sources.

## Key Changes

- **Integration model**
  - Run Telegram ingestion in the existing worker process using **Bot API long polling**, not webhooks.
  - Use one shared Telegram bot per Sentinel deployment, configured through backend settings.
  - Add a lightweight Telegram integration subsystem that:
    - polls updates
    - ignores unsupported update types
    - normalizes supported messages into the existing capture contract
    - enqueues normal capture processing
- **User linking**
  - Add an authenticated Telegram linking flow in Sentinel:
    - user opens a Telegram section in settings
    - user requests a short-lived link code
    - user sends that code to the shared Telegram bot
    - backend binds that Telegram private chat to the authenticated Sentinel user
  - Support one active Telegram private chat binding per Sentinel user in v1.
  - Support unlink/relink from Sentinel settings.
- **Telegram scope**
  - Ingest only **private chat** messages sent to the linked bot.
  - Ignore group, supergroup, channel, and inline contexts in v1.
  - Accept only **text messages and text messages containing URLs**.
  - Ignore attachments, voice notes, photos, documents, stickers, and other media updates in v1.
- **Capture normalization**
  - Reuse `POST /api/v1/capture` semantics internally rather than inventing a separate processing path.
  - Map Telegram messages to normal captures with:
    - `ContentType = Note`
    - `RawContent =` message text, trimmed to existing limits
    - `SourceUrl =` first detected URL when present, otherwise empty
    - `Metadata =` JSON containing at least `source = "telegram"`, `importSource = "telegram_bot"`, `telegramChatId`, `telegramUserId`, `telegramMessageId`, `telegramUpdateId`, `receivedAt`, and sender/chat display data when available
    - default `tags` include `telegram`
    - default `labels` include `Source = Telegram`
  - Keep existing LLM extraction, embeddings, search, assistant, and clustering flows unchanged after capture creation.
- **Backend/API/interfaces**
  - Add authenticated Telegram integration endpoints, for example:
    - start link-code issuance
    - fetch current Telegram link status
    - unlink current Telegram chat
  - Add persistence for:
    - Telegram chat binding to Sentinel user
    - short-lived link codes
    - last processed Telegram `update_id` cursor for the bot
  - Add configuration for:
    - bot token
    - polling cadence / batch size
    - link-code TTL
- **Frontend**
  - Add a Telegram section to the authenticated settings area showing:
    - linked / unlinked state
    - current link code when requested
    - expiry countdown or expiry state
    - unlink action
  - Keep the UX explicitly Telegram-first; do not add WhatsApp or Signal UI in v1 beyond optional “planned / not available yet” copy.
- **Deferred follow-ons**
  - **WhatsApp** follow-up should be modeled as a separate feature using official Business/Cloud API onboarding, webhook delivery, and its own user/account linking flow.
  - **Signal** follow-up should remain deferred until the product intentionally allows unofficial/local bridge tooling; do not design v1 around that path.

## Important Interfaces and Types

- New authenticated integration endpoints under an integrations/authenticated area, not under public webhook routes.
- New persisted integration records for:
  - Telegram user/chat binding
  - Telegram link code issuance
  - Telegram poll cursor / checkpoint
- New worker-side Telegram polling service abstraction that converts Telegram updates into `CaptureRequestDto`-compatible internal requests.
- No change to the public capture ingestion contract is required for v1.
- No new `ContentType` is required in v1; Telegram messages use existing `Note`.

## Test Plan

- Backend unit tests:
  - link-code generation, expiry, and single-use consumption
  - Telegram update filtering for private-chat text vs unsupported update types
  - normalization to capture request with correct tags, labels, metadata, and URL extraction
  - duplicate protection using stored `update_id` / message identifiers
- Backend integration tests:
  - linked Telegram chat creates owner-scoped captures
  - unlinked chats do not create captures
  - expired or invalid link codes do not bind chats
  - unlink stops future ingestion from that chat
  - multi-user isolation is preserved across separate Telegram bindings
- Worker tests:
  - long polling resumes from stored cursor
  - transient Telegram API failures retry without duplicating accepted messages
- Frontend tests:
  - settings page shows unlinked, code-issued, linked, and expired states correctly
  - unlink action updates visible state
- Manual scenarios:
  - issue code in Sentinel, send it to bot, verify chat becomes linked
  - send plain text and text-with-link to bot, verify captures appear and process normally
  - send media, verify it is ignored without breaking polling

## Assumptions and Defaults

- V1 uses **long polling in the worker** because it fits the current always-on worker architecture and avoids adding a new public webhook ingress path.
- V1 assumes a **single shared Telegram bot token per deployment**, not one bot per user.
- V1 is intentionally **private-chat only** and **text/URL only**.
- Telegram messages are treated as normal knowledge captures, not as a separate searchable entity type.
- **WhatsApp** is postponed because the official route is heavier and business-oriented, not because the Sentinel backend cannot ingest it.
- **Signal** is postponed because the current direction is **official-only**, and official bot-style ingestion is not available.
