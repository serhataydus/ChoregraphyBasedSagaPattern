using Microsoft.AspNetCore.Mvc;

namespace Order.WebApi.Controllers;

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
