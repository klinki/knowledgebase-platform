FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj", "src/SentinelKnowledgebase.Api/"]
COPY ["src/SentinelKnowledgebase.Application/SentinelKnowledgebase.Application.csproj", "src/SentinelKnowledgebase.Application/"]
COPY ["src/SentinelKnowledgebase.Domain/SentinelKnowledgebase.Domain.csproj", "src/SentinelKnowledgebase.Domain/"]
COPY ["src/SentinelKnowledgebase.Infrastructure/SentinelKnowledgebase.Infrastructure.csproj", "src/SentinelKnowledgebase.Infrastructure/"]
RUN dotnet restore "src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj"
COPY . .
RUN dotnet build "src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /src
COPY ["src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj", "src/SentinelKnowledgebase.Api/"]
COPY ["src/SentinelKnowledgebase.Application/SentinelKnowledgebase.Application.csproj", "src/SentinelKnowledgebase.Application/"]
COPY ["src/SentinelKnowledgebase.Domain/SentinelKnowledgebase.Domain.csproj", "src/SentinelKnowledgebase.Domain/"]
COPY ["src/SentinelKnowledgebase.Infrastructure/SentinelKnowledgebase.Infrastructure.csproj", "src/SentinelKnowledgebase.Infrastructure/"]
COPY ["tests/SentinelKnowledgebase.IntegrationTests/SentinelKnowledgebase.IntegrationTests.csproj", "tests/SentinelKnowledgebase.IntegrationTests/"]
COPY ["tests/SentinelKnowledgebase.UnitTests/SentinelKnowledgebase.UnitTests.csproj", "tests/SentinelKnowledgebase.UnitTests/"]
RUN dotnet restore "tests/SentinelKnowledgebase.IntegrationTests/SentinelKnowledgebase.IntegrationTests.csproj"
RUN dotnet restore "tests/SentinelKnowledgebase.UnitTests/SentinelKnowledgebase.UnitTests.csproj"
COPY . .
RUN dotnet test "tests/SentinelKnowledgebase.IntegrationTests/SentinelKnowledgebase.IntegrationTests.csproj" --no-build --verbosity normal
RUN dotnet test "tests/SentinelKnowledgebase.UnitTests/SentinelKnowledgebase.UnitTests.csproj" --no-build --verbosity normal

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
COPY --from=build /app/build .
RUN dotnet publish "src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "SentinelKnowledgebase.Api.dll"]
