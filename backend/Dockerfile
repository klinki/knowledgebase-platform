# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY SentinelKnowledgebase.Domain/*.csproj SentinelKnowledgebase.Domain/
COPY SentinelKnowledgebase.Infrastructure/*.csproj SentinelKnowledgebase.Infrastructure/
COPY SentinelKnowledgebase.Application/*.csproj SentinelKnowledgebase.Application/
COPY SentinelKnowledgebase.API/*.csproj SentinelKnowledgebase.API/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish SentinelKnowledgebase.API/SentinelKnowledgebase.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SentinelKnowledgebase.API.dll"]
