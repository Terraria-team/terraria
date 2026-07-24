namespace Lobby.Application.Contracts.ExternalServices;


public interface ITokenService
{
    string GenerateRandomToken();
    string HashToken(string token);
}
