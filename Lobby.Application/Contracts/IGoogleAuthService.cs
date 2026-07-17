using Lobby.Application.Models;

namespace Lobby.Application.Contracts;

public interface IGoogleAuthService
{
    Task<ResultModel<PlayerGoogleLoginModel>> ExchangeCode(string code, string redirectUri);
}