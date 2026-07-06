namespace Lobby.Application.Contracts;

public interface ITokenService
{
    string GenerateRandomToken();
    string HashToken(string token);
}