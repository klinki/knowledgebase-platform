# Implementation Plan - Unified Build Script

I will implement a unified build script using **PowerShell** (`build.ps1`). PowerShell is the best choice for this environment (Windows) as it is native, requires no additional installations (unlike `make`), and provides robust handling of both `npm` and `dotnet` toolchains.

The script will be designed with a "Task-based" approach, mimicking the target-based structure of a Makefile.

## Proposed Changes

### Root Directory

#### [NEW] [build.ps1](file:///c:/ai-workspace/knowledgebase-platform/build.ps1)
A PowerShell script that will serve as the entry point for all build tasks.

**Supported Commands:**
- `./build.ps1` (Default: Builds both Backend and Frontend)
- `./build.ps1 -Target Backend` (Builds only the .NET projects)
- `./build.ps1 -Target Frontend` (Builds only the browser extension)
- `./build.ps1 -Target Clean` (Cleans build artifacts for both)

**Implementation Details:**
- Use `dotnet build` for the backend.
- Use `npm run build` for the frontend (requires `cd` or `--prefix`).
- Implement color-coded output for better readability.
- Basic error handling (exit on failure).

## Verification Plan

### Automated Tests
1. **Full Build**: Run `./build.ps1` and verify that both `backend` binaries and `browser-extension/dist` are generated.
2. **Targeted Backend Build**: Run `./build.ps1 -Target Backend` and verify only backend artifacts are updated.
3. **Targeted Frontend Build**: Run `./build.ps1 -Target Frontend` and verify only frontend artifacts are updated.
4. **Clean Task**: Run `./build.ps1 -Target Clean` and verify `bin/obj` folders in backend and `dist` in frontend are removed.

### Manual Verification
- None required beyond running the script and checking the output/files.
