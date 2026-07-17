using Lobby.Application.Contracts;
using Lobby.Mappers;
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
    public async Task<IActionResult> Create()
    {
        var created = await _instanceService.Create();
        return Created($"/server-instances", created);
    }
}
