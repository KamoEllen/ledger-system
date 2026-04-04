# ─────────────────────────────────────────────────────────────────────────────
# Stage 1 — restore & build
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + project files first for layer-cached restore
COPY LedgerSystem.sln ./
COPY src/LedgerSystem.Domain/LedgerSystem.Domain.csproj             src/LedgerSystem.Domain/
COPY src/LedgerSystem.Application/LedgerSystem.Application.csproj   src/LedgerSystem.Application/
COPY src/LedgerSystem.Infrastructure/LedgerSystem.Infrastructure.csproj src/LedgerSystem.Infrastructure/
COPY src/LedgerSystem.API/LedgerSystem.API.csproj                   src/LedgerSystem.API/
COPY tests/LedgerSystem.UnitTests/LedgerSystem.UnitTests.csproj     tests/LedgerSystem.UnitTests/
COPY tests/LedgerSystem.IntegrationTests/LedgerSystem.IntegrationTests.csproj tests/LedgerSystem.IntegrationTests/

RUN dotnet restore

# Copy the full source and publish
COPY src/ src/
COPY tests/ tests/

RUN dotnet publish src/LedgerSystem.API/LedgerSystem.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2 — runtime (smallest possible image)
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system --gid 1001 appgroup \
 && adduser  --system --uid 1001 --ingroup appgroup --no-create-home appuser

# Copy published artefacts from the build stage
COPY --from=build /app/publish ./

# Create logs directory owned by the app user
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app/logs

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LedgerSystem.API.dll"]
