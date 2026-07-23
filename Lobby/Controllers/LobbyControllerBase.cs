using Lobby.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lobby.Controllers;


public class LobbyControllerBase : ControllerBase
{
 
    protected IActionResult HttpError(ErrorModel error)
    {
        switch (error.ErrorType)
        {
            case ErrorType.Conflict:
                return Conflict(error.ErrorMessage);
            case ErrorType.NotFound:
                return NotFound(error.ErrorMessage);
            case ErrorType.Validation:
                return BadRequest(error.ErrorMessage);
            case ErrorType.Unauthorized:
                return BadRequest(error.ErrorMessage);
            case ErrorType.UnexpectedError:
                return StatusCode(500, "Internal Server Error");
            case ErrorType.ResourceExhausted:
                return StatusCode(503, error.ErrorMessage);
            default:
                return StatusCode(500, "Internal Server Error");
        }
    }
}