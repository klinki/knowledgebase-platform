# Feature Specification: Unified Build Script

## Goal
Implement a unified build script that orchestrates building both the backend (.NET) and the browser extension (npm/tsc), providing a single entry point for developers.

## Scope
- **Universal Entry Point**: Create a `build.ps1` script in the root directory.
- **Multi-Target Support**: Support targets for Backend, Frontend, and Clean.
- **Cross-Platform Readiness**: Use PowerShell for native Windows support.
- **Developer Ergonomics**: Support environment checks, selective dev startup,
  and dependency bootstrap without unnecessary reinstall churn.

## Acceptance Criteria
- [x] A `build.ps1` script exists in the repository root.
- [x] Running `./build.ps1` builds both the backend and frontend.
- [x] Running `./build.ps1 -Target Backend` builds only the backend solution.
- [x] Running `./build.ps1 -Target WebFrontend` builds only the Angular web
      frontend.
- [x] Running `./build.ps1 -Target Extension` builds only the browser
      extension.
- [x] Running `./build.ps1 -Target Clean` cleans build artifacts for both projects.
- [x] The script provides clear, color-coded output.
- [x] The script handles errors and exits with a non-zero code on failure.
- [x] Running `./build.ps1 -Target Setup` installs .NET, frontend, and extension
      dependencies, creates `backend/.env` from the template when missing,
      prompts for required secrets such as `OPENAI_API_KEY`, and installs
      Playwright Chromium for both web and extension projects.
- [x] Running `./build.ps1 -Target Check` validates local prerequisites and
      reports expected dev URLs and likely port conflicts.
- [x] Running `./build.ps1 -Target Dev` starts the standard local environment
      without reinstalling npm dependencies when they already exist.
- [x] Running `./build.ps1 -Target Dev` supports selectively skipping API,
      worker, frontend, extension watch, or infrastructure startup.

## Implementation Status
### Phase 1: Planning & Setup
- [x] [DONE] Research existing build processes.
- [x] [DONE] Create feature specification.

### Phase 2: Implementation
- [x] [DONE] Implement `build.ps1` script with target logic.
- [x] [DONE] Implement Backend build logic (`dotnet build`).
- [x] [DONE] Implement Frontend build logic (`npm install && npm run build`).
- [x] [DONE] Implement Clean logic.
- [x] [DONE] Add one-time setup/bootstrap behavior for Playwright and node
      dependencies.
- [x] [DONE] Add backend `.env` bootstrap and required secret prompting to
      `Setup`.
- [x] [DONE] Add environment check target and dev URL reporting.
- [x] [DONE] Add selective dev startup flags and skip repeated npm installs
      during `Dev`.

### Phase 3: Verification
- [x] [DONE] Verify Frontend target.
- [x] [DONE] Verify Backend target.
- [x] [DONE] Verify Clean target.
- [x] [DONE] Verify Check target.
- [x] [DONE] Verify Setup target installs frontend and extension Playwright
      Chromium and prepares `backend/.env`.
- [x] [DONE] Verify Dev target honors skip flags and aligns Angular dev API URL
      with the local backend endpoint.

## Verification Plan
- [x] **Full Build**: Run `./build.ps1` and verify artifacts.
- [x] **Modular Build**: Run specific targets and verify only relevant artifacts are updated.
- [x] **Clean**: Run the clean target and verify artifacts are removed.
- [x] **Check**: Run `./build.ps1 -Target Check` and verify local prerequisite
      reporting.
- [x] **Setup**: Run `./build.ps1 -Target Setup` and verify it creates
      `backend/.env` from the template and prompts for missing required values.
- [x] **Dev**: Run `./build.ps1 -Target Dev` and verify it starts the default
      local environment without reinstalling node dependencies on every run.
