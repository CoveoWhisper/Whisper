using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            return this.Ok(new ApiVersion());
        }
    }
}