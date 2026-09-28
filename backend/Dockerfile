# Cloudflare Containers run linux/amd64. The SDK stage runs on the build
# machine's own architecture and cross-publishes, so an Apple Silicon Mac
# does not have to emulate the whole .NET build.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY FriendsCorner.Core/FriendsCorner.Core.csproj FriendsCorner.Core/
COPY FriendsCorner.Infrastructure/FriendsCorner.Infrastructure.csproj FriendsCorner.Infrastructure/
COPY FriendsCorner.Api/FriendsCorner.Api.csproj FriendsCorner.Api/
RUN dotnet restore FriendsCorner.Api/FriendsCorner.Api.csproj -a $TARGETARCH

COPY FriendsCorner.Core/ FriendsCorner.Core/
COPY FriendsCorner.Infrastructure/ FriendsCorner.Infrastructure/
COPY FriendsCorner.Api/ FriendsCorner.Api/
RUN dotnet publish FriendsCorner.Api/FriendsCorner.Api.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "FriendsCorner.Api.dll"]
