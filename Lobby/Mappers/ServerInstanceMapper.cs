using Lobby.Application.Entities;
using LobbyUnityShared.DTOs;
using Riok.Mapperly.Abstractions;

namespace Lobby.Mappers;

[Mapper]
public static partial class ServerInstanceMapper
{
    [MapperIgnoreSource(nameof(ServerInstanceEntity.World))]
    public static partial ServerInstanceDto Map(ServerInstanceEntity entity);
}