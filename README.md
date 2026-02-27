# Sentinel Knowledge Engine

Sentinel is an advanced knowledge curation platform that bypasses traditional API limitations using a browser-resident agent. It enables users to capture high-signal content (Tweets, Web Articles, Selections) directly from their browsing session, processes it using state-of-the-art LLMs, and stores it in a searchable, vector-indexed vault.

## Project Overview

The project consists of a **Chrome Extension** that acts as the data harvester and a robust **.NET 10 Backend** that manages the ingestion, processing, and semantic retrieval of information.

### Core Features

- **Seamless Capture**: Inject "Save to Sentinel" buttons directly into web platforms (X.com, etc.).
- **AI-Powered Insights**: Automated de-noising, summary extraction, and actionable insight generation using OpenAI Models.
- **Semantic Search**: Meaning-based retrieval using vector embeddings stored in PostgreSQL.
- **Reliable Processing**: Persistent background job management with Hangfire.
- **Full Observability**: Structured logging with Serilog/Seq and metrics via OpenTelemetry.

## Tech Stack

### Backend
- **Framework**: .NET 10.0 (ASP.NET Core)
- **Database**: PostgreSQL with `pgvector` extension
- **Background Jobs**: Hangfire (PostgreSQL storage)
- **Observability**: Serilog (Seq Sink), OpenTelemetry (Metrics), Health Checks
- **Testing**: xUnit, FluentAssertions, NSubstitute, Testcontainers (PostgreSQL)

### AI Layer
- **Processing**: OpenAI `gpt-4o` / `gpt-4o-mini`
- **Embeddings**: OpenAI `text-embedding-3-small` (1536-dimensional vectors)

### Frontend
- **Browser Extension**: Manifest V3, TypeScript, Chrome Storage & Scripting APIs

## Quick Start

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- OpenAI API Key

### 1. Start Infrastructure
Launch the database, Seq (logging), and other services using Docker:
```bash
docker-compose up -d
```

### 2. Configure the Backend
Set your OpenAI API Key and connection strings in `backend/src/SentinelKnowledgebase.Api/appsettings.json` or via environment variables:
```bash
$env:OPENAI__ApiKey = "your-key-here"
```

### 3. Run the Application
```bash
cd backend
dotnet build
dotnet run --project src/SentinelKnowledgebase.Api
```

### 4. Explore
- **Swagger API**: `https://localhost:5001/swagger`
- **Hangfire Dashboard**: `https://localhost:5001/hangfire` (Job monitoring)
- **Health Checks**: `https://localhost:5001/health`
- **Seq (Logs)**: `http://localhost:5341` (Local logging UI)
