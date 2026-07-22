using Application;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
            return Ok(new { success = true, message = "تمت العملية بنجاح" });
        
        return BadRequest(new { success = false, error = result.Error, errorCode = result.ErrorCode });
    }

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(new { success = true, data = result.Value });
        
        return BadRequest(new { success = false, error = result.Error, errorCode = result.ErrorCode });
    }

    protected int GetUserId()
    {
        var claim = User.FindFirst("UserId");
        return claim != null ? int.Parse(claim.Value) : 0;
    }

}
