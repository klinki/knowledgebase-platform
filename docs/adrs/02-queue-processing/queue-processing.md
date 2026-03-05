# ADR 02: Queue Processing

## Status
Accepted

## Context
Initially, the Sentinel Knowledgebase backend used in-memory background processing for content ingestion and analysis. This was implemented using ASP.NET Core `BackgroundService` (Hosted Services) and `System.Threading.Channels` as an internal queue.

While sufficient for early development, this approach had several limitations:
1. **Lack of Persistence**: Enqueued jobs were lost upon application restart or crash.
2. **Limited Observability**: No built-in way to monitor queue depth, job history, or failure rates without custom implementation.
3. **Manual Resilience**: Retries and error handling had to be manually coded for each secondary task.

## Decision
We decided to replace the in-memory queue system with **Hangfire** using **PostgreSQL** as the persistent storage backend.
We also run Hangfire workers in a **dedicated worker process** (`SentinelKnowledgebase.Worker`) instead of starting workers inside the API process.

### Initialization Model (Updated March 1, 2026)

- **API (`SentinelKnowledgebase.Api`)**
  - Configures Hangfire storage (`AddHangfire(...)`) for job enqueueing and dashboard access.
  - Does **not** call `AddHangfireServer()`.
  - Remains responsive even if Hangfire worker startup or storage checks are slow.

- **Worker (`SentinelKnowledgebase.Worker`)**
  - Configures the same Hangfire storage/retry policy as API.
  - Calls `AddHangfireServer()` and executes background jobs.
  - Can be scaled independently from API instances.

### Comparison: Hosted Services vs. Hangfire

| Feature | Legacy Hosted Services (In-Memory) | Hangfire (PostgreSQL) |
|---------|------------------------------------|-----------------------|
| **Persistence** | None (Lost on restart) | Robust (Stored in SQL) |
| **Observability**| Log-only | Dashboard UI + Storage API |
| **Retries** | Manual implementation | Automated with configurable policy |
| **Scale-out** | Single-instance only | Multiple workers across instances |
| **Complexity** | Low (Built-in) | Moderate (External dependency) |

## Reasoning

The primary driver for this transition was **Reliability**. For a knowledgebase platform where users expect their captured content to be processed eventually, losing tasks during deployment or minor failures was unacceptable.

1. **Persistence**: Jobs survive server restarts, ensuring no data loss.
2. **Observability**: The Hangfire Dashboard provides immediate visibility into the "Sentinel Ingestion Engine" without requiring custom monitoring code.
3. **Automated Retries**: AI processing (OpenAI API calls) is prone to transient failures (rate limits, timeouts). Hangfire's built-in retry mechanism handles these gracefully.
4. **Simplification of Core Logic**: Moving queue management to a dedicated library allowed us to focus on the `CaptureService` domain logic rather than infrastructure boilerplate.

## Consequences

- **Storage Requirement**: High-availability PostgreSQL is now required for both domain data and background job state.
- **Dependency**: The project now depends on `Hangfire.AspNetCore` and `Hangfire.PostgreSql`.
- **Resource Management**: Background workers consume database connections; connection pool sizes must be monitored.
- **Observability Alignment**: Health checks moved from checking an in-memory channel to checking Hangfire storage availability (`HangfireStorageHealthCheck`).
- **Process Split**: The API and Hangfire worker are now separate deployable processes/containers.
- **Startup Reliability**: API startup is no longer blocked by Hangfire server initialization or transient worker-storage startup delays.
- **Operational Complexity**: Deployments must ensure both `api` and `worker` services are running.
