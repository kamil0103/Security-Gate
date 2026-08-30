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

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apk add --no-cache wget
ENTRYPOINT ["dotnet", "SecurityGateway.Api.dll"]
