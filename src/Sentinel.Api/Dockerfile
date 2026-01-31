FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Sentinel.Knowledgebase.sln", "./"]
COPY ["src/Sentinel.Api/Sentinel.Api.csproj", "src/Sentinel.Api/"]
COPY ["src/Sentinel.Application/Sentinel.Application.csproj", "src/Sentinel.Application/"]
COPY ["src/Sentinel.Domain/Sentinel.Domain.csproj", "src/Sentinel.Domain/"]
COPY ["src/Sentinel.Infrastructure/Sentinel.Infrastructure.csproj", "src/Sentinel.Infrastructure/"]

RUN dotnet restore "src/Sentinel.Api/Sentinel.Api.csproj"

COPY . .
RUN dotnet publish "src/Sentinel.Api/Sentinel.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sentinel.Api.dll"]
