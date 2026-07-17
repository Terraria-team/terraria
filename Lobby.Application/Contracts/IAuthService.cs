using Lobby.Application.Models;

namespace Lobby.Application.Contracts;

public interface IAuthService
{
    Task<ResultModel<LoginTokensModel>> LoginWithGoogle(string code, string redirectUri, string createdByIp);
    
    Task<ResultModel> Logout(Guid playerId, string refreshToken);
    
    Task LogoutAll(Guid playerId);
    
    Task<ResultModel<LoginTokensModel>> Refresh(string refreshToken, string createByIp);
}