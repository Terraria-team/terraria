using System.Security.Claims;
using Lobby.Application.Contracts;
using LobbyUnityShared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lobby.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : LobbyControllerBase
{
    private readonly IAuthService _authService;
    

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    

    [HttpPost("google-login")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto record )
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress!.MapToIPv4();
        var res = await _authService.LoginWithGoogle(record.Code, record.RedirectUri, ipAddress.ToString());
        return res.IsSuccessful ? 
            Ok(new LoginTokensDto()
        {
            AccessToken = res.Result!.accessToken,
            RefreshToken = res.Result!.sessionToken
        }) 
            : HttpError(res.Error!);
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshCookie([FromBody] RefreshTokenDto record)
    {

        var ipAddress = HttpContext.Connection.RemoteIpAddress!.MapToIPv4();

        var result = await _authService.Refresh(record.RefreshToken, ipAddress.ToString());
        if (!result.IsSuccessful) return HttpError(result.Error!);
        
        return Ok(new LoginTokensDto(){AccessToken = result.Result!.accessToken, RefreshToken = result.Result!.sessionToken});
    }
    
    [Authorize]
    [HttpPost("logoutAll")]
    public async Task<IActionResult> LogoutAll()
    {
        var playerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(playerIdClaim, out var playerId))
            return Unauthorized();

        await _authService.LogoutAll(playerId);
        
        return NoContent();
    }
    
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto record)
    {
        var playerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(playerIdClaim, out var playerId))
            return Unauthorized();

        await _authService.Logout(playerId, record.RefreshToken);

        return NoContent();
    }
    
}