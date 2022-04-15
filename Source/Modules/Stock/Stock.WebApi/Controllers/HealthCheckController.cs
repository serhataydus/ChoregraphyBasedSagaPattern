using Microsoft.AspNetCore.Mvc;

namespace Stock.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthCheckController : ControllerBase
{
    [HttpGet(Name = "Get")]
    public bool Get()
    {
        return true;
    }
}
