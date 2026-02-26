# Feature Specification: Observability

## Goal
Establish a robust observability framework for the Sentinel Knowledgebase backend to ensure high reliability, performance monitoring, and rapid troubleshooting.

## Scope
- **Structured Logging**: Implement Serilog with Seq sink for centralized log management and analysis.
- **Metrics**: Implement OpenTelemetry to track key performance indicators (KPIs) and technical metrics.
- **Health Checks**: Add health monitoring for the API and background processing worker.
- **Infrastructure**: Use OpenTelemetry as the standardized telemetry protocol to ensure compatibility with Grafana and Seq.

## Acceptance Criteria
- [ ] Serilog is configured in `Program.cs` and used across all application layers.
- [ ] Logs are successfully exported to a Seq instance (configurable via environment variables).
- [ ] OpenTelemetry is configured to export metrics (HTTP latencies, exception counts, business metrics).
- [ ] Custom business metrics are tracked:
    - Capture request processing duration.
    - AI token usage (input/output/total).
    - Embedding generation latency.
- [ ] `/health` endpoint is available and reports the status of the API and its dependencies (Database, Background Processing).
- [ ] Dashboard is draftable in Grafana/Seq using the exported telemetry.

## Implementation Status
### Phase 1: Infrastructure & Logging
- [ ] [TODO] Add following NuGet packages:
    - `Serilog.AspNetCore`
    - `Serilog.Sinks.Seq`
    - `Serilog.Sinks.Console`
- [ ] [TODO] Configure Serilog in `Program.cs` using `appsettings.json` configuration.
- [ ] [TODO] Replace standard `ILogger` usage with structured logging patterns where applicable.

### Phase 2: OpenTelemetry Metrics
- [ ] [TODO] Add NuGet packages:
    - `OpenTelemetry.Extensions.Hosting`
    - `OpenTelemetry.Instrumentation.AspNetCore`
    - `OpenTelemetry.Instrumentation.Http`
    - `OpenTelemetry.Instrumentation.Runtime`
    - `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- [ ] [TODO] Configure OpenTelemetry for Metrics in `Program.cs`.
- [ ] [TODO] Implement a `MonitoringService` or similar to track custom business metrics (Capture throughput, AI Costs).

### Phase 3: Health Checks
- [ ] [TODO] Configure ASP.NET Core Health Checks.
- [ ] [TODO] Add checks for PostgreSQL connection.
- [ ] [TODO] Add a custom check for the `CaptureProcessingQueue` state.

## Verification Plan
- [ ] **Logging Test**: Trigger a log event and verify its appearance in both Console and Seq UI.
- [ ] **Metrics Test**: Verify `/metrics` or OTLP export activity using a local collector (Jaeger/Prometheus/Seq).
- [ ] **Health Test**: Call `GET /health` and verify `200 OK` when healthy and `503 Service Unavailable` when a dependency is down.
