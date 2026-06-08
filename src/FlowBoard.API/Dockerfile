# Multi-stage build — keeps the runtime image small and free of SDK tooling
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first to leverage Docker layer caching for restore
COPY ["src/FlowBoard.API/FlowBoard.API.csproj", "src/FlowBoard.API/"]
COPY ["src/FlowBoard.Application/FlowBoard.Application.csproj", "src/FlowBoard.Application/"]
COPY ["src/FlowBoard.Domain/FlowBoard.Domain.csproj", "src/FlowBoard.Domain/"]
COPY ["src/FlowBoard.Infrastructure/FlowBoard.Infrastructure.csproj", "src/FlowBoard.Infrastructure/"]
RUN dotnet restore "src/FlowBoard.API/FlowBoard.API.csproj"

# Copy everything else and publish
COPY . .
WORKDIR "/src/src/FlowBoard.API"
RUN dotnet publish "FlowBoard.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image — ASP.NET Core only, no SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as non-root for defense in depth
RUN useradd --create-home --shell /bin/bash appuser
USER appuser

COPY --from=build --chown=appuser:appuser /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FlowBoard.API.dll"]