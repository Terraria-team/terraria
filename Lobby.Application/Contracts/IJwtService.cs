namespace Lobby.Application.Contracts;

public interface IJwtService
{
    string GenerateToken(Guid playerId, string email, string role, string name);
}