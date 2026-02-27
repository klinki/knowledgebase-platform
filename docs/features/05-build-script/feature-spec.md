# Feature Specification: Unified Build Script

## Goal
Implement a unified build script that orchestrates building both the backend (.NET) and the browser extension (npm/tsc), providing a single entry point for developers.

## Scope
- **Universal Entry Point**: Create a `build.ps1` script in the root directory.
- **Multi-Target Support**: Support targets for Backend, Frontend, and Clean.
- **Cross-Platform Readiness**: Use PowerShell for native Windows support.

## Acceptance Criteria
- [x] A `build.ps1` script exists in the repository root.
- [x] Running `./build.ps1` builds both the backend and frontend.
- [x] Running `./build.ps1 -Target Backend` builds only the backend solution.
- [x] Running `./build.ps1 -Target Frontend` builds only the browser extension.
- [x] Running `./build.ps1 -Target Clean` cleans build artifacts for both projects.
- [x] The script provides clear, color-coded output.
- [x] The script handles errors and exits with a non-zero code on failure.

## Implementation Status
### Phase 1: Planning & Setup
- [x] [DONE] Research existing build processes.
- [x] [DONE] Create feature specification.

### Phase 2: Implementation
- [x] [DONE] Implement `build.ps1` script with target logic.
- [x] [DONE] Implement Backend build logic (`dotnet build`).
- [x] [DONE] Implement Frontend build logic (`npm install && npm run build`).
- [x] [DONE] Implement Clean logic.

### Phase 3: Verification
- [x] [DONE] Verify Frontend target.
- [x] [DONE] Verify Backend target.
- [x] [DONE] Verify Clean target.

## Verification Plan
- [x] **Full Build**: Run `./build.ps1` and verify artifacts.
- [x] **Modular Build**: Run specific targets and verify only relevant artifacts are updated.
- [x] **Clean**: Run the clean target and verify artifacts are removed.
