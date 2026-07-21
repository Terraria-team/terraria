using System.Security.Claims;
using Lobby.Application.Contracts;
using Lobby.Filters;
using Lobby.Mappers;
using LobbyUnityShared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lobby.Controllers;


[ApiController]
[Authorize]
[Route("api/server-instances")]
public class ServerInstancesController : LobbyControllerBase
{
    private readonly IServerInstanceService _instanceService;
    
    
    public ServerInstancesController( IServerInstanceService instanceService)
    {
        _instanceService = instanceService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var serverInstanceEntities = await _instanceService.GetAll();
        return Ok(serverInstanceEntities.Select(ServerInstanceMapper.Map));
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServerDto dto)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid playerId))
        {
            return Unauthorized("Invalid or missing user ID in token.");
        }
        
        var created = await _instanceService.Create(dto.Name, playerId);
        return Created($"/server-instances/{created.Id}", ServerInstanceMapper.Map(created)); 
    }
}
