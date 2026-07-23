using Lobby.Application.Domain;

namespace Lobby.Application.Contracts.Repositories;

public interface IPlayerExternalIdentityRepository
{
    Task<PlayerExternalIdentityEntity?> GetByExternalIdAsync(string provider, string externalId);

    Task AddAsync(PlayerExternalIdentityEntity identity);
}
