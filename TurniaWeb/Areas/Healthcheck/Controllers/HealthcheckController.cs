using Microsoft.AspNetCore.Mvc;

namespace TurniaWeb.Areas.Healthcheck.Controllers
{
    [Area("Healthcheck")]
    [ApiController]
    public class HealthcheckController : Controller
    {
        [HttpGet("/healthcheck")]
        public IActionResult Healthcheck()
        {
            return Ok();
        }
    }
}
