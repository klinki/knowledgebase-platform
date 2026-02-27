# Feature Specification: Build Script - Run Target

## Goal
Add a `Run` target to the `build.ps1` script to automate the setup of the development environment and launch a browser with the extension pre-loaded for immediate testing/development.

## Scope
- **Environment Automation**: The target should ensure Docker containers (PostgreSQL, etc.) are running.
- **Browser Launch**: Use Playwright (or a similar lightweight mechanism) to open a Chromium instance with the Sentinel extension loaded from the `dist` directory.
- **Integration**: Orchestrate existing build steps (Frontend build) before launching.

## Acceptance Criteria
- [ ] Running `./build.ps1 -Target Run` starts Docker containers (defined in `backend/docker-compose.yml`).
- [ ] Running `./build.ps1 -Target Run` ensures the latest browser extension is built.
- [ ] Running `./build.ps1 -Target Run` opens a non-headless Chromium browser.
- [ ] The Sentinel extension is active and visible in the launched browser.
- [ ] The script remains responsive (doesn't hang indefinitely without feedback).

## Implementation Status
### Phase 1: Infrastructure & Automation
- [ ] Implement Docker orchestration in `build.ps1` (`docker-compose up -d`).
- [ ] Implement Browser launch script (e.g., via a small Node.js helper using Playwright).
- [ ] Integrate the `Run` target into the `build.ps1` switch statement.

### Phase 2: Verification
- [ ] Verify Docker containers start correctly.
- [ ] Verify browser opens with extension loaded.

## Verification Plan
- [ ] **Docker Check**: Run target and verify `docker ps` shows running containers.
- [ ] **Browser Check**: Interact with a tweet in the launched browser to verify extension functionality.
