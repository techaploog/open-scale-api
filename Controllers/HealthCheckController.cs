using Microsoft.AspNetCore.Mvc;

namespace fpn_scale_api.Controllers
  {
  [ApiController]
  [Route("/")]
  public class HealthCheckController : ControllerBase
    {
    [HttpGet]
    public IActionResult GetHealthStatus()
      {
      return Ok(new
        {
        success = true,
        status = "Healthy",
        timestamp = DateTime.UtcNow
        });
      }
    }
  }
