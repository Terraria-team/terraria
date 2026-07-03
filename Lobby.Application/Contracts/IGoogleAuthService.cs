using Lobby.Application.Models;

namespace Lobby.Application.Contracts;

public interface IGoogleAuthService
{
    Task<ResultModel<PlayerGoogleLoginModel>> ValidateToken(string idToken);
}