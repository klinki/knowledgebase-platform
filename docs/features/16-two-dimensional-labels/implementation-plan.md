# Execution Plan: 2D Labels With Parallel Workstreams

## Summary

- **Feature slug:** `16-two-dimensional-labels`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/16-two-dimensional-labels/implementation-plan.md`
- Add a new label system that remains **separate from existing tags**.
- Labels are **free-form category/value pairs** per user.
- Each raw capture and each processed insight may have **at most one value per category**.
- V1 includes:
  - label catalog CRUD
  - label assignment during capture creation
  - auto-fill from known metadata (`Source`, `Language`)
  - label display in dashboard and capture detail views
  - exact category+value label search
  - browser extension support
- Processed insights get their **own persisted label assignments**, copied from raw captures during processing.

## Implementation Changes

### Backend core

- Add new domain entities:
  - `LabelCategory`
  - `LabelValue`
  - `RawCaptureLabelAssignment`
  - `ProcessedInsightLabelAssignment`
- Extend [RawCapture.cs](/backend/src/SentinelKnowledgebase.Domain/Entities/RawCapture.cs) and [ProcessedInsight.cs](/backend/src/SentinelKnowledgebase.Domain/Entities/ProcessedInsight.cs) with label-assignment navigations.
- Update [ApplicationDbContext.cs](/backend/src/SentinelKnowledgebase.Infrastructure/Data/ApplicationDbContext.cs) with:
  - unique `(OwnerUserId, Name)` on categories
  - unique `(LabelCategoryId, Value)` on values
  - unique `(RawCaptureId, LabelCategoryId)` on raw assignments
  - unique `(ProcessedInsightId, LabelCategoryId)` on processed assignments
- Generate a new EF migration plus snapshot update under [backend/src/SentinelKnowledgebase.Migrations/Migrations](/backend/src/SentinelKnowledgebase.Migrations/Migrations).

### Backend services and contracts

- Extend [CaptureDto.cs](/backend/src/SentinelKnowledgebase.Application/DTOs/Capture/CaptureDto.cs) so capture requests accept:
  - `tags?: string[]`
  - `labels?: { category: string; value: string }[]`
- Extend capture, dashboard, and search response DTOs to return `labels` alongside existing `tags`.
- Add new labels DTOs under `DTOs/Labels/`.
- Add validation for labels:
  - non-empty category/value after trim
  - max length `100`
  - no duplicate categories within one request
- Add label resolution and assignment logic in [CaptureService.cs](/backend/src/SentinelKnowledgebase.Application/Services/CaptureService.cs):
  - explicit request labels
  - auto-filled `Source` and `Language`
  - explicit request wins over auto-fill for the same category
- During `ProcessCaptureAsync`, copy raw label assignments into processed-insight label assignments before save completes.
- Add repositories and unit-of-work wiring in:
  - [IRepositories.cs](/backend/src/SentinelKnowledgebase.Infrastructure/Repositories/IRepositories.cs)
  - [UnitOfWork.cs](/backend/src/SentinelKnowledgebase.Infrastructure/Repositories/UnitOfWork.cs)
  - new label repositories
- Add `LabelService` and `ILabelService`.
- Add [LabelsController.cs](/backend/src/SentinelKnowledgebase.Api/Controllers/LabelsController.cs) with:
  - `GET /api/v1/labels`
  - `POST /api/v1/labels/categories`
  - `PATCH /api/v1/labels/categories/{id}`
  - `DELETE /api/v1/labels/categories/{id}`
  - `POST /api/v1/labels/categories/{id}/values`
  - `PATCH /api/v1/labels/values/{id}`
  - `DELETE /api/v1/labels/values/{id}`
- Add `POST /api/v1/search/labels` in [SearchController.cs](/backend/src/SentinelKnowledgebase.Api/Controllers/SearchController.cs), backed by processed-insight label assignments only.
- Keep `/api/v1/tags` and `/api/v1/search/tags` unchanged.
- Refresh OpenAPI after backend contract changes land.

### Frontend

- Update shared models in [knowledge.model.ts](/frontend/src/app/shared/models/knowledge.model.ts) to add label shapes without removing tags.
- Add labels state in a new [labels-state.service.ts](/frontend/src/app/core/services/labels-state.service.ts).
- Update:
  - [capture-state.service.ts](/frontend/src/app/core/services/capture-state.service.ts)
  - [dashboard-state.service.ts](/frontend/src/app/core/services/dashboard-state.service.ts)
  - [search-state.service.ts](/frontend/src/app/core/services/search-state.service.ts)
- Add new labels page at [labels.component.ts](/frontend/src/app/features/labels/labels.component.ts).
- Wire `/labels` route in [app.routes.ts](/frontend/src/app/app.routes.ts) and add shell navigation in [shell.component.ts](/frontend/src/app/features/shell/shell.component.ts).
- Update [create-capture.component.ts](/frontend/src/app/features/captures/create-capture.component.ts):
  - keep tags input
  - add repeatable label rows
  - prevent duplicate categories client-side
- Update [capture-detail.component.ts](/frontend/src/app/features/captures/capture-detail.component.ts) and [dashboard.component.ts](/frontend/src/app/features/dashboard/dashboard.component.ts) to render `Category: Value` chips alongside tags.
- Keep semantic search UI unchanged; label search lives on the labels page.

### Browser extension

- Update [background.ts](/browser-extension/src/background.ts) only:
  - add `labels` to `CaptureRequestPayload`
  - keep `tags` unchanged
  - tweet => `Source=Twitter`
  - webpage and selection => `Source=Web`
  - webpage => add `Language` from existing `metadata.language` when present
  - selection does not add `Language` in v1 unless page-language capture is separately implemented
- Refresh [openapi.json](/browser-extension/openapi.json) from backend output after API changes are complete.

## Parallel Execution

### Critical path

1. Backend schema and contract layer
2. Backend service/controller implementation
3. OpenAPI refresh
4. Frontend and extension contract consumers
5. Tests and verification

### Worker split

1. **Worker A: Backend schema + repositories**
   - Ownership:
     - domain label entities
     - [ApplicationDbContext.cs](/backend/src/SentinelKnowledgebase.Infrastructure/Data/ApplicationDbContext.cs)
     - migration files
     - repository interfaces/implementations
     - unit-of-work wiring
   - Must finish before backend service/controller work can compile cleanly.

2. **Worker B: Backend services + API**
   - Ownership:
     - DTOs
     - validators
     - [CaptureService.cs](/backend/src/SentinelKnowledgebase.Application/Services/CaptureService.cs)
     - [DashboardService.cs](/backend/src/SentinelKnowledgebase.Application/Services/DashboardService.cs)
     - [SearchService.cs](/backend/src/SentinelKnowledgebase.Application/Services/SearchService.cs)
     - new `LabelService`
     - [SearchController.cs](/backend/src/SentinelKnowledgebase.Api/Controllers/SearchController.cs)
     - new `LabelsController.cs`
   - Coordinate with Worker A on final repository shapes; do not edit migration files.

3. **Worker C: Frontend contracts + labels page**
   - Ownership:
     - [knowledge.model.ts](/frontend/src/app/shared/models/knowledge.model.ts)
     - new `labels-state.service.ts`
     - [app.routes.ts](/frontend/src/app/app.routes.ts)
     - [shell.component.ts](/frontend/src/app/features/shell/shell.component.ts)
     - new `labels.component.ts`
   - Starts after backend DTO shape is stable enough to mirror.

4. **Worker D: Frontend capture/dashboard/detail integration**
   - Ownership:
     - [capture-state.service.ts](/frontend/src/app/core/services/capture-state.service.ts)
     - [create-capture.component.ts](/frontend/src/app/features/captures/create-capture.component.ts)
     - [capture-detail.component.ts](/frontend/src/app/features/captures/capture-detail.component.ts)
     - [dashboard.component.ts](/frontend/src/app/features/dashboard/dashboard.component.ts)
     - related unit specs
   - Coordinate with Worker C on shared frontend model definitions; do not edit labels page files.

5. **Worker E: Browser extension + contract snapshot**
   - Ownership:
     - [background.ts](/browser-extension/src/background.ts)
     - extension tests
     - [openapi.json](/browser-extension/openapi.json)
   - Starts after backend OpenAPI is generated; do not hand-edit schema assumptions before backend contract is finalized.

### Coordination rules

- Workers are **not alone in the codebase** and must not revert each other’s changes.
- Worker A owns schema/repository files exclusively.
- Worker B owns backend service/controller/DTO/validator files exclusively.
- Worker C owns labels route/page/state files exclusively.
- Worker D owns capture/dashboard/detail frontend files exclusively.
- Worker E owns extension files and the OpenAPI snapshot exclusively.
- Tag behavior is a regression boundary: no worker should refactor tag logic beyond compatibility-preserving DTO/render updates.

## Test Plan

- Backend unit:
  - label normalization and duplicate-category validation
  - auto-fill precedence
  - processed-insight label copying
  - exact-pair label search with `matchAll`
- Backend integration:
  - label CRUD
  - owner scoping
  - capture creation with labels
  - processed insight returns copied labels
  - label deletion removes raw and processed assignments
  - tag endpoints still pass unchanged
- Frontend unit:
  - capture form duplicate-category validation and payload mapping
  - label rendering on dashboard/detail
  - labels page CRUD/search states
- Frontend e2e:
  - `/labels` route and empty/error states
  - fixture updates for mixed `tags` + `labels`
- Extension tests:
  - tweet/webpage/selection payload mapping
  - `Source` and `Language` label emission where applicable
- Verification commands after implementation:
  - backend tests covering unit + integration
  - frontend tests
  - browser-extension tests
  - regenerate API contract artifact
  - build affected projects

## Assumptions

- Existing captures are not backfilled with labels.
- Label search is exact category+value matching only in v1.
- Dashboard summary widgets remain tag-based in v1.
- Selection captures get `Source=Web` but not `Language` unless extra page-language plumbing is added.
- Migration SQL must be validated explicitly because the integration fixture relies on `EnsureCreated` and will not prove migration correctness.
- Actual code changes can start only after Plan Mode ends; this plan is the execution contract for that next step.
