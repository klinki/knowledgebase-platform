# Sentinel Knowledgebase Architecture

## Overview
Sentinel Knowledgebase is a multi-component system for capturing, processing, and retrieving high-signal knowledge artifacts.

The platform is composed of:
- Browser Extension (capture client)
- Backend API (`SentinelKnowledgebase.Api`)
- Background Worker (`SentinelKnowledgebase.Worker`)
- PostgreSQL + `pgvector` (operational + vector storage)
- Angular Frontend (dashboard and discovery UI)

## High-Level Flow
1. User captures content in the browser extension (tweet/webpage/selection).
2. Extension sends capture payload to `POST /api/v1/capture`.
3. API persists raw capture data and enqueues processing via Hangfire.
4. Worker processes jobs asynchronously:
   - content cleanup and analysis
   - insight extraction
   - embedding generation
   - persistence of processed insight + vector
5. Frontend and API search endpoints query structured records + vectors for retrieval.

## Runtime Topology
- `SentinelKnowledgebase.Api`:
  - HTTP surface (controllers, OpenAPI/Scalar, health checks)
  - Hangfire storage configuration
  - no in-process Hangfire server
- `SentinelKnowledgebase.Worker`:
  - Hangfire server host
  - executes background processing jobs
- PostgreSQL:
  - domain entities
  - Hangfire state and jobs
  - vector similarity data (`pgvector`)

## Architectural Principles
- API responsiveness is prioritized by running job execution in a separate worker process.
- Capture ingestion contract is normalized at boundaries (extension -> API DTO).
- Domain/application/infrastructure separation is preserved in backend projects.
- Documentation and implementation evolve together (docs-as-code workflow).

## System Entity Model

For the implemented persisted entities, lifecycle states, and cross-component
data flows, use [`/docs/ENTITY-MODEL.md`](/docs/ENTITY-MODEL.md) as the canonical
high-level model. It complements this runtime overview with:

- entity relationships across ingestion and authentication
- lifecycle states derived from enums and persisted timestamps
- end-to-end flow diagrams for capture, processing, search, and sign-in

## Key Documentation
- Project status: `/docs/STATUS.md`
- Decisions (ADRs): `/docs/adrs/`
- Feature specs: `/docs/features/`
- System entity model: `/docs/ENTITY-MODEL.md`
- Queue processing decision: `/docs/adrs/02-queue-processing/queue-processing.md`
- Deployment edge routing decision: `/docs/adrs/03-deployment-edge-routing/deployment-edge-routing.md`
