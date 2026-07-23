using Lobby.Application.Contracts;
using Lobby.Filters;
using Lobby.Mappers;
using LobbyUnityShared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Lobby.Controllers;

[ApiController]
[Route("api/internal/server-instances")]
[TypeFilter(typeof(LobbyToServerAuthFilter))]
public class InternalServerInstancesController : LobbyControllerBase
{
    private readonly IServerInstanceService _instanceService;
    
    
    public InternalServerInstancesController( IServerInstanceService instanceService)
    {
        _instanceService = instanceService;
    }
    
    [HttpPost("{id:guid}/player-count")]
    public async Task<IActionResult> UpdatePlayerCount([FromBody] UpdatePlayerCountDto dto, [FromRoute] Guid id)
    {
        if (dto.PlayerCount < 0) return BadRequest("player count must be >= 0");
        
        var created = await _instanceService.UpdatePlayerCount(id, dto.PlayerCount);
        return created.IsSuccessful? Ok(ServerInstanceMapper.Map(created.Result!)) : HttpError(created.Error!);
    }
    
}