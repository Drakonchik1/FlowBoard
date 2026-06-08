# Multi-stage build — keeps the runtime image small and free of SDK tooling
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FlowBoard.API/FlowBoard.API.csproj", "src/FlowBoard.API/"]
COPY ["src/FlowBoard.Application/FlowBoard.Application.csproj", "src/FlowBoard.Application/"]
COPY ["src/FlowBoard.Domain/FlowBoard.Domain.csproj", "src/FlowBoard.Domain/"]
COPY ["src/FlowBoard.Infrastructure/FlowBoard.Infrastructure.csproj", "src/FlowBoard.Infrastructure/"]
RUN dotnet restore "src/FlowBoard.API/FlowBoard.API.csproj"

COPY . .
WORKDIR "/src/src/FlowBoard.API"
RUN dotnet publish "FlowBoard.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl for container health checks (aspnet image has no wget)
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN useradd --create-home --shell /bin/bash appuser
USER appuser

COPY --from=build --chown=appuser:appuser /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "FlowBoard.API.dll"]
