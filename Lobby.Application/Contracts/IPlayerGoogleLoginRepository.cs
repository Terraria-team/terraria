using Lobby.Application.Entities;

namespace Lobby.Application.Contracts;

public interface IPlayerGoogleLoginRepository
{
    Task<PlayerGoogleLoginEntity?> GetById(string googleId);
}