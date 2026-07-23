namespace Lobby.Application.Contracts.ExternalServices;

public interface IJwtService
{
    string GenerateToken(Guid playerId, string email, string role, string name);
}