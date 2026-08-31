# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/SecurityGateway.Api/SecurityGateway.Api.csproj"
RUN dotnet build "src/SecurityGateway.Api/SecurityGateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/SecurityGateway.Api/SecurityGateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false
# Install dotnet-ef into a known tool path and create a migration bundle.
RUN dotnet tool install dotnet-ef --version 9.0.0 --tool-path /src/.dotnet-tools && \
    /src/.dotnet-tools/dotnet-ef migrations bundle \
        --startup-project src/SecurityGateway.Api \
        --project src/SecurityGateway.Infrastructure \
        --output /app/efbundle

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/efbundle .
RUN apk add --no-cache wget && \
    addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -s /bin/sh -D appuser && \
    chown -R appuser:appgroup /app

COPY <<EOF /app/entrypoint.sh
#!/bin/sh
set -e

echo "Applying database migrations..."
/app/efbundle

echo "Starting Security Gateway..."
exec dotnet SecurityGateway.Api.dll
EOF
RUN chmod +x /app/entrypoint.sh && \
    chown appuser:appgroup /app/entrypoint.sh

USER appuser
ENTRYPOINT ["/app/entrypoint.sh"]
