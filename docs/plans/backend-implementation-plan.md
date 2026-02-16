# Sentinel Backend Implementation Plan (.NET)

## Overview

High-level implementation plan for the Sentinel backend using .NET 8, ASP.NET Core, EF Core with PostgreSQL/pgvector, and xUnit for testing.

## Architecture

Browser Extension → ASP.NET Core API → Services → EF Core → PostgreSQL + pgvector

## Technology Stack

| Layer | Technology |
|-------|------------|
| Framework | .NET 8, ASP.NET Core |
| Database | PostgreSQL + pgvector extension |
| ORM | Entity Framework Core |
| AI/LLM | Azure.AI.OpenAI SDK |
| Validation | FluentValidation |
| Logging | Serilog |
| Testing | xUnit, FluentAssertions, Testcontainers |
| Documentation | Swagger/OpenAPI |

## Project Structure

Clean Architecture with 4 layers:

1. **Sentinel.Domain** - Entities, enums
2. **Sentinel.Application** - Services, DTOs, validators, interfaces
3. **Sentinel.Infrastructure** - Data access, external APIs, repositories
4. **Sentinel.API** - Controllers, middleware, configuration

Plus test projects:
- **Sentinel.IntegrationTests** - API endpoint tests with Testcontainers
- **Sentinel.UnitTests** - Service logic tests

## Core Components

### Domain Entities

- **RawCapture** - Original captured content (tweet, article)
- **ProcessedInsight** - LLM-extracted insights with vector embedding
- **ProcessingStatus** - Pending, Processing, Completed, Failed

### API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | /api/v1/capture | Accept new capture |
| GET | /api/v1/capture/{id} | Get processed insight |
| POST | /api/v1/search/semantic | Semantic search |
| POST | /api/v1/search/tags | Search by tags |

### Services

**ICaptureService**: Orchestrates the capture and processing pipeline
- CaptureAsync(): Validates, stores, triggers async processing
- GetInsightAsync(): Retrieves processed insight
- SearchAsync(): Semantic search using pgvector
- SearchByTagsAsync(): Tag-based search

**IDeNoiserService**: Cleans raw content
- CleanTweet(): Removes thread markers, excessive whitespace
- ExtractUrls(): Extracts URLs from content
- RemoveTrackingParameters(): Strips UTM params, fbclid, etc.

**IOpenAIService**: LLM integration
- ExtractInsightsAsync(): Calls GPT-4o-mini for summary, insight, sentiment, tags
- GenerateEmbeddingAsync(): Calls text-embedding-3-small for vector generation

### Data Access

**SentinelDbContext**: EF Core DbContext
- Configures RawCapture and ProcessedInsight entities
- Sets up pgvector column (vector(1536) for OpenAI embeddings)
- Configures indexes for performance

**Repositories**:
- IRawCaptureRepository: CRUD operations, duplicate detection
- IProcessedInsightRepository: CRUD, vector similarity search, tag filtering

## Implementation Phases

### Phase 1: Project Setup
**Objective**: Initialize solution structure and dependencies

**Key Packages**:
- Sentinel.API: Swashbuckle.AspNetCore, Serilog.AspNetCore, FluentValidation.AspNetCore
- Sentinel.Infrastructure: Npgsql.EntityFrameworkCore.PostgreSQL, Pgvector.EntityFrameworkCore, Azure.AI.OpenAI
- Tests: Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql, FluentAssertions

**Verification**: `dotnet build` compiles successfully

---

### Phase 2: Domain Layer
**Objective**: Define core entities and enums

**Components**:
- RawCapture entity with navigation to ProcessedInsight
- ProcessedInsight entity with vector embedding support
- ProcessingStatus enum (Pending, Processing, Completed, Failed)

**Verification**: `dotnet build` succeeds

---

### Phase 3: Data Access Layer
**Objective**: Configure EF Core and repositories

**Components**:
- SentinelDbContext with entity configurations
- Repository interfaces and implementations
- EF Core migrations

**Key Configuration**:
- PostgreSQL connection with pgvector extension
- Vector column: `vector(1536)` for OpenAI text-embedding-3-small
- Unique index on SourceId for duplicate detection

**Verification**: 
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
# Tables created in PostgreSQL
```

---

### Phase 4: Application Services
**Objective**: Implement business logic and DTOs

**Components**:
- DTOs: CaptureRequest, CaptureResponse, InsightResponse, SearchRequest
- Validators: FluentValidation rules for input validation
- Services: CaptureService, DeNoiserService, OpenAIService

**Processing Pipeline**:
1. Validate input → Return 202 Accepted
2. Store RawCapture → Trigger async processing
3. De-noise content → Remove thread markers, tracking params
4. Call OpenAI → Extract summary, insight, sentiment, tags
5. Generate embedding → Vector for semantic search
6. Store ProcessedInsight → Mark as Completed

**Verification**: Unit tests pass for DeNoiserService, validators

---

### Phase 5: API Layer
**Objective**: Implement controllers and configure DI

**Components**:
- CaptureController: POST /capture, GET /capture/{id}
- SearchController: POST /search/semantic, POST /search/tags
- Program.cs: Service registration, Swagger, Serilog

**Configuration**:
- OpenAI API key from configuration
- PostgreSQL connection string
- FluentValidation auto-validation

**Verification**: 
```bash
dotnet run
# Swagger UI accessible at /swagger
# POST /api/v1/capture returns 202
```

---

### Phase 6: Integration Tests (xUnit)
**Objective**: Test API endpoints with real database

**Test Infrastructure**:
- IntegrationTestFixture: Manages WebApplicationFactory + Testcontainers PostgreSQL
- Collection definition for sharing fixture across tests

**Test Scenarios**:
- CaptureControllerTests:
  - Valid request returns 202 Accepted
  - Duplicate SourceId returns 409 Conflict
  - Invalid request returns 400 BadRequest
  - Non-existent ID returns 404 NotFound

- SearchControllerTests:
  - Semantic search returns results
  - Tag search returns filtered results

**Verification**: `dotnet test` passes all integration tests

---

### Phase 7: Unit Tests (xUnit)
**Objective**: Test service logic in isolation

**Test Scenarios**:
- DeNoiserServiceTests:
  - Thread marker removal (Thread 1/n, 1/5, etc.)
  - URL extraction from content
  - Tracking parameter removal (utm_*, fbclid, gclid)

- CaptureRequestValidatorTests:
  - Valid request passes
  - Missing required fields fail
  - Invalid SourceType fails

**Verification**: `dotnet test` passes all unit tests

---

### Phase 8: Deployment
**Objective**: Containerize and deploy

**Docker Compose**:
- PostgreSQL with pgvector extension
- API service with environment variables

**Environment Variables**:
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `OpenAI__ApiKey`: OpenAI API key

**Verification**:
```bash
docker-compose up --build
# API accessible at http://localhost:8080
# POST /api/v1/capture returns 202
```

## Verification Summary

| Phase | Verification Command | Expected Result |
|-------|---------------------|-----------------|
| 1 | `dotnet build` | Compiles without errors |
| 2 | `dotnet build` | Domain layer compiles |
| 3 | `dotnet ef database update` | Tables created |
| 4 | `dotnet test` (unit) | Unit tests pass |
| 5 | `dotnet run` | API starts, Swagger accessible |
| 6 | `dotnet test` (integration) | Integration tests pass |
| 7 | `dotnet test` (unit) | Unit tests pass |
| 8 | `docker-compose up` | Services start, API responds |

## Next Steps After Implementation

1. **CI/CD Pipeline**: GitHub Actions or Azure DevOps for automated builds/tests
2. **Authentication**: API key or JWT-based auth for production
3. **Rate Limiting**: Prevent API abuse
4. **Health Checks**: `/health` endpoint for monitoring
5. **Logging**: Application Insights or similar for production logging
6. **Frontend Dashboard**: Phase 4 - React/Vue web interface
