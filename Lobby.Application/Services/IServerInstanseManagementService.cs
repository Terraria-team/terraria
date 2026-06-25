using Lobby.Application.Entities;

namespace Lobby.Application.Services;

public interface IServerInstanseManagementService
{
    Task<DockerContainerInfo> CreateNewServerInstance(int port, string name);
}