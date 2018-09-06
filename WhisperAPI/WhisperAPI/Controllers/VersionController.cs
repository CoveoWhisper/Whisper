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
            string versionJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { version = Version.Value});
            return this.Ok(versionJson);
        }
    }
}