# Bug Status

## Current State
investigating

## Active Attempt
`fix-attempt-001.md`

## Last Updated
2026-04-11 - worker startup fix in progress

## Confirmation Date

## Resolution Summary

## Attempt History
- `fix-attempt-001.md` - switch recurring job registration to DI-based Hangfire API

## State Change Log
- 2026-04-11: bug opened
- 2026-04-11: confirmed worker uses static `RecurringJob.AddOrUpdate(...)` during startup

## Notes
- Failure occurs before normal worker processing begins.
