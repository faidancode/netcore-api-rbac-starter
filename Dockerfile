# ============================================================
# Stage 1: Build
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer cache optimization)
COPY *.csproj ./
RUN dotnet restore

# Copy source code and publish
COPY . ./
RUN dotnet publish ./netcore-api-rbac-starter.csproj -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================
# Stage 2: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd --system appgroup \
    && useradd --system --gid appgroup --create-home --home-dir /home/appuser appuser

# Create logs directory with correct permissions
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Copy published output from build stage
COPY --from=build --chown=appuser:appgroup /app/publish ./

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5000

# Configure ASP.NET to listen on port 5000
ENV ASPNETCORE_URLS=http://+:5000

# Healthcheck — hits /health endpoint (adjust if different)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "netcore-api-rbac-starter.dll"]
