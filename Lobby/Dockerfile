# ── runtime base ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ── build ────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first so NuGet restore is cached separately from source changes
COPY ["Lobby/Lobby.csproj",                         "Lobby/"]
COPY ["Lobby.Application/Lobby.Application.csproj", "Lobby.Application/"]
COPY ["Lobby.Infrastructure/Lobby.Infrastructure.csproj", "Lobby.Infrastructure/"]
COPY ["LobbyUnityShared/LobbyUnityShared.csproj",   "LobbyUnityShared/"]
COPY ["LobbyUnityShared/Directory.Build.props",      "LobbyUnityShared/"]

RUN dotnet restore "Lobby/Lobby.csproj"

# Copy source after restore so the restore layer is reused on code-only changes
COPY Lobby/             Lobby/
COPY Lobby.Application/ Lobby.Application/
COPY Lobby.Infrastructure/ Lobby.Infrastructure/
COPY LobbyUnityShared/  LobbyUnityShared/

WORKDIR "/src/Lobby"
RUN dotnet build "Lobby.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ── publish ──────────────────────────────────────────────────────────────────────
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Lobby.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    /p:UseAppHost=false --no-restore

# ── final image ──────────────────────────────────────────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Secrets must be injected at runtime via environment variables, for example:
#   -e Authentication__Google__ClientId=...
#   -e Authentication__Google__ClientSecret=...
#   -e Authentication__Jwt__Secret=...
#   -e ConnectionStrings__LobbyDb=...

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Lobby.dll"]