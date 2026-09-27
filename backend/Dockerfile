# Cloudflare Containers run linux/amd64. The SDK stage runs on the build
# machine's own architecture and cross-publishes, so an Apple Silicon Mac
# does not have to emulate the whole .NET build.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY DistanceTogether.Room/DistanceTogether.Room.csproj DistanceTogether.Room/
COPY DistanceTogether.Storage/DistanceTogether.Storage.csproj DistanceTogether.Storage/
COPY DistanceTogether.Server/DistanceTogether.Server.csproj DistanceTogether.Server/
RUN dotnet restore DistanceTogether.Server/DistanceTogether.Server.csproj -a $TARGETARCH

COPY DistanceTogether.Room/ DistanceTogether.Room/
COPY DistanceTogether.Storage/ DistanceTogether.Storage/
COPY DistanceTogether.Server/ DistanceTogether.Server/
RUN dotnet publish DistanceTogether.Server/DistanceTogether.Server.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DistanceTogether.Server.dll"]
