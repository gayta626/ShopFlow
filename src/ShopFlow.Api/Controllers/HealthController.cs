using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Data;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ShopFlowDbContext _dbContext;

    public HealthController(ShopFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unhealthy" });
        }

        return Ok(new { status = "healthy" });
    }
}
