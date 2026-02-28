# Implementation Plan: Sentinel Angular Frontend (v2)

This document outlines the architectural and modular rollout for the Sentinel web dashboard.

## Finalized Architecture
- **Zoneless Change Detection**: Using `provideZonelessChangeDetection()` for modern performance.
- **Reactive State**: **Angular Signals** serve as the backbone for data flow in services.
- **SOC**: High separation between `core/` (services, guards), `shared/` (models, components, animations), and `features/` (page-level components).

## Completed Phases

### Phase 1: Foundations [DONE]
- [x] Initialized Angular shell with SCSS design system.
- [x] Implemented glassmorphism tokens and global styles.

### Phase 2: Auth & Layout [DONE]
- [x] Built the centered, premium login viewport.
- [x] Established the main app shell with a modern sidebar.
- [x] Implemented `AuthService` and route protection.

### Phase 3: Dashboard & Search [DONE]
- [x] Refactored data management into `KnowledgeService`.
- [x] Built the large hero search bar with instant filtering.
- [x] Validated tag cloud and recent items widgets.

### Phase 4: Modern Features & QA [DONE]
- [x] Enabled **Angular Animations** for page transitions.
- [x] Converted to **Zoneless** mode to remove `Zone.js` dependency.
- [x] Wrote and verified the Playwright E2E suite.

### Phase 5: Backend Integration [DONE]
- [x] **Search API**: Wired Hero Search to the `api/v1/search/semantic` endpoint.
- [x] **Environment Config**: Refactored hardcoded URLs into Angular environment files.
- [x] **Test Hardening**: Implemented backend mocking in Playwright using `page.route`.
- [x] **Error Handling**: Signal-based loading and error states with mock fallbacks.

## Future Considerations
- Integration with the C# backend API.
- Real-time notification system for new captures.
- Advanced tag editing and merging tools.
