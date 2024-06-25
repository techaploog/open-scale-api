using Microsoft.AspNetCore.Mvc;
using fpn_scale_api.Services;
using System.Threading.Tasks;

namespace fpn_scale_api.Controllers
  {
  [ApiController]
  [Route("[controller]")]
  public class ScaleController : ControllerBase
    {
    private readonly SerialPortService _serialPortService;
    private readonly SerialPortSettings _settings;

    public ScaleController(SerialPortService serialPortService, SerialPortSettings settings)
      {
      _serialPortService = serialPortService;
      _settings = settings;
      }

    [HttpGet("{uuid}")]
    public async Task<IActionResult> GetScaleData(string uuid, [FromQuery] int? baudRate)
      {
      var actualBaudRate = baudRate ?? _settings.DefaultBaudRate;
      var (success, message, value, unit, warning) = await _serialPortService.GetConsistentData(uuid, actualBaudRate);

      if (success)
        {
        return Ok(new
          {
          success,
          scaleId = uuid,
          data = new
            {
            weight = value,
            unit
            },
          warning
          });
        }
      else
        {
        return BadRequest(new
          {
          success,
          message
          });
        }
      }

    [HttpGet("{uuid}/health")]
    public async Task<IActionResult> GetScaleHealth(string uuid)
      {
      var actualBaudRate = _settings.DefaultBaudRate;
      var (success, message, value, unit, warning) = await _serialPortService.GetConsistentData(uuid, actualBaudRate);

      if (success)
        {
        return Ok(new
          {
          success,
          uuid,
          status = "Healthy",
          value,
          unit,
          warning,
          timestamp = DateTime.UtcNow
          });
        }
      else
        {
        return BadRequest(new
          {
          success,
          message
          });
        }
      }
    }
  }
