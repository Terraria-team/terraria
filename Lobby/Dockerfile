FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Lobby/Lobby.csproj", "Lobby/"]
COPY ["LobbyUnityShared/LobbyUnityShared.csproj", "LobbyUnityShared/"]
COPY ["LobbyUnityShared/Directory.Build.props", "LobbyUnityShared/"]
RUN dotnet restore "Lobby/Lobby.csproj"

COPY Lobby/ Lobby/
COPY LobbyUnityShared/ LobbyUnityShared/
WORKDIR "/src/Lobby"

RUN dotnet build "./Lobby.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Lobby.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lobby.dll"]