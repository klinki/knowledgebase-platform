# Project Specification: Sentinel Knowledge Engine

## 1. Executive Summary

Sentinel is a platform designed to bypass the limitations of the X (Twitter) API by using a browser-resident agent. It captures high-signal social media content, processes it via LLMs to extract "raw value," and stores it in a searchable, vector-indexed knowledge base.

## 2. Technical Stack

- **Extension:** JavaScript/TypeScript (Chrome Manifest V3)
- **Backend:** Node.js (FastAPI/Express)
- **AI Layer:** OpenAI `gpt-4o-mini` (Processing) & `text-embedding-3-small` (Search)
- **Database:** PostgreSQL + `pgvector` (via Supabase or local instance)

---

## 3. Structured Implementation Plan

### Phase 1: The Collector (Browser Extension)

**Goal:** Create the interface to capture data directly from the user's browser session.

| Task ID | Task Name | Description |
|---|---|---|
| **1.1** | **Manifest Setup** | Initialize `manifest.json` with permissions for `storage`, `scripting`, and host permissions for `x.com`. |
| **1.2** | **DOM Injection** | Create a Content Script that uses a `MutationObserver` to watch for new tweets and injects a "Save to Sentinel" button into the `[data-testid="reply"]` button group. |
| **1.3** | **Data Scraper** | Logic to extract Tweet ID, Text, Author, and Timestamp from the DOM when the button is clicked. |
| **1.4** | **[Optional] Shadow API** | Implement a background worker to intercept `/__graphql/` requests. Match the JSON response objects to the current Tweet ID for richer metadata collection. |
| **1.5** | **Auth Bridge** | Create an options page to allow the user to input their Sentinel API Key. |

### Phase 2: The Ingestion Engine (API & Pipeline)

**Goal:** Receive raw data and transform it into structured knowledge.

| Task ID | Task Name | Description |
|---|---|---|
| **2.1** | **Ingest Endpoint** | Build a secure `POST /api/v1/capture` endpoint to receive payloads from the extension. |
| **2.2** | **The De-Noiser** | Create a cleaning utility to strip "Thread 1/n," unnecessary emojis, and tracking parameters from URLs. |
| **2.3** | **LLM Extraction** | Send the cleaned text to an LLM with a system prompt to extract: Summary, Core Insight, and Sentiment. |
| **2.4** | **Taxonomy Engine** | Use the LLM to auto-assign 3-5 tags based on a dynamic or predefined category list. |

### Phase 3: The Vault (Storage & Retrieval)

**Goal:** Save data in a way that makes it "findable" by meaning.

| Task ID | Task Name | Description |
|---|---|---|
| **3.1** | **Database Schema** | Design tables: `raw_captures` (source data) and `processed_insights` (refined data). |
| **3.2** | **Vectorization** | Generate embeddings for every processed insight using the OpenAI Embeddings API. |
| **3.3** | **Semantic Search** | Implement a search endpoint using a vector similarity function (Cosine Similarity). |

### Phase 4: Discovery Dashboard (UI)

**Goal:** A central hub to browse and interact with the knowledge base.

| Task ID | Task Name | Description |
|---|---|---|
| **4.1** | **Insight Feed** | Build a clean, card-based UI showing the original tweet and the AI-extracted "Raw Value." |
| **4.2** | **Semantic Filter** | Allow users to search the knowledge base using natural language (e.g., "Find tips about SaaS marketing"). |
| **4.3** | **Export Module** | Add "Copy to Markdown" or "Send to Notion" functionality for individual insights. |

---

## 4. Key Logic Flows

### Data Acquisition Flow

1. User scrolls X.com → Extension injects button.
2. User clicks "Save" → Extension captures DOM/Shadow API data.
3. Extension sends JSON to Backend API.

### AI Processing Pipeline (The "Brain")

1. **Input:** Raw Tweet String.
2. **Step A:** LLM generates a 1-sentence "Bottom Line Up Front" (BLUF).
3. **Step B:** LLM extracts actionable bullets.
4. **Step C:** Embedding model converts result to a vector of numbers.
5. **Output:** Stored in DB for future retrieval.

---

## 5. Success Metrics

- **Latency:** Capture to DB storage in under 3 seconds.
- **Accuracy:** AI-generated tags match user intent 85% of the time.
- **Reliability:** Extension successfully renders the button on 95% of loaded tweets.

## Sentinel Knowledgebase - Backend API

ASP.NET Core backend for the Sentinel Knowledgebase platform.

### Tech Stack

- **Framework:** ASP.NET Core 8.0
- **Database:** PostgreSQL + pgvector
- **ORM:** Entity Framework Core
- **Testing:** xUnit with Testcontainers

### Project Structure

```text
src/
├── SentinelKnowledgebase.Api/          # API Layer
├── SentinelKnowledgebase.Application/  # Application Services
├── SentinelKnowledgebase.Domain/       # Domain Entities
└── SentinelKnowledgebase.Infrastructure/ # Data Access
tests/
├── SentinelKnowledgebase.IntegrationTests/
└── SentinelKnowledgebase.UnitTests/
```

### API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/capture` | Accept new capture |
| GET | `/api/v1/capture/{id}` | Get processed insight |
| POST | `/api/v1/search/semantic` | Semantic search |
| POST | `/api/v1/search/tags` | Search by tags |

### Quick Start

```bash
# Build
dotnet build

# Run with Docker
docker-compose up -d

# Run tests
dotnet test
```

### Configuration

Set `OPENAI_API_KEY` environment variable for AI processing.
