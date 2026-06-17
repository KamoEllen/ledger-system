FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY LedgerSystem.sln .
COPY src/LedgerSystem.Domain/LedgerSystem.Domain.csproj src/LedgerSystem.Domain/
COPY src/LedgerSystem.Application/LedgerSystem.Application.csproj src/LedgerSystem.Application/
COPY src/LedgerSystem.Infrastructure/LedgerSystem.Infrastructure.csproj src/LedgerSystem.Infrastructure/
COPY src/LedgerSystem.API/LedgerSystem.API.csproj src/LedgerSystem.API/

RUN dotnet restore src/LedgerSystem.API/LedgerSystem.API.csproj

COPY src/ src/

RUN dotnet publish src/LedgerSystem.API/LedgerSystem.API.csproj \
    -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "LedgerSystem.API.dll"]
