# Cloudflare Containers run linux/amd64. The SDK stage runs on the build
# machine's own architecture and cross-publishes, so an Apple Silicon Mac
# does not have to emulate the whole .NET build.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY FriendsCorner.Room/FriendsCorner.Room.csproj FriendsCorner.Room/
COPY FriendsCorner.Storage/FriendsCorner.Storage.csproj FriendsCorner.Storage/
COPY FriendsCorner.Server/FriendsCorner.Server.csproj FriendsCorner.Server/
RUN dotnet restore FriendsCorner.Server/FriendsCorner.Server.csproj -a $TARGETARCH

COPY FriendsCorner.Room/ FriendsCorner.Room/
COPY FriendsCorner.Storage/ FriendsCorner.Storage/
COPY FriendsCorner.Server/ FriendsCorner.Server/
RUN dotnet publish FriendsCorner.Server/FriendsCorner.Server.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "FriendsCorner.Server.dll"]
