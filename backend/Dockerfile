# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers for better caching
COPY ["src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj", "src/SentinelKnowledgebase.Api/"]
COPY ["src/SentinelKnowledgebase.Application/SentinelKnowledgebase.Application.csproj", "src/SentinelKnowledgebase.Application/"]
COPY ["src/SentinelKnowledgebase.Domain/SentinelKnowledgebase.Domain.csproj", "src/SentinelKnowledgebase.Domain/"]
COPY ["src/SentinelKnowledgebase.Infrastructure/SentinelKnowledgebase.Infrastructure.csproj", "src/SentinelKnowledgebase.Infrastructure/"]

RUN dotnet restore "src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/SentinelKnowledgebase.Api"
RUN dotnet build "SentinelKnowledgebase.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SentinelKnowledgebase.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "SentinelKnowledgebase.Api.dll"]
