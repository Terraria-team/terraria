using System.Security.Cryptography;
using System.Text;
using Lobby.Application.Contracts;

namespace Lobby.Infrastructure.Services;

public class TokenService : ITokenService
{
    
    public string GenerateRandomToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
    
    public string HashToken(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }   
}