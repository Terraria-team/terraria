using Lobby.Application.Models;

namespace Lobby.Application.Contracts.ExternalServices;


public interface IExternalAuthProvider
{
    string ProviderName { get; }
    
    Task<ResultModel<PlayerExternalIdentityModel>> VerifyIdentityAsync(string code, string redirectUri);
}
