using Lobby.Application.Entities;
using LobbyUnityShared.DTOs;
using Riok.Mapperly.Abstractions;

namespace Lobby.Mappers;

[Mapper]
public static partial class ServerInstanceMapper
{
    public static partial ServerInstanceDto Map(ServerInstanceEntity entity);
}