using Lobby.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lobby.Controllers;


[ApiController]
[Route("server-instances")]
public class ServerInstancesController : ControllerBase
{
    private readonly IServerInstanceService _instanceService;
    
    public ServerInstancesController( IServerInstanceService instanceService)
    {
        _instanceService = instanceService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var difficulties = await _instanceService.GetAll();
        return Ok(difficulties);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var created = await _instanceService.Create();
        return Created($"/server-instances", created);
    }
}
