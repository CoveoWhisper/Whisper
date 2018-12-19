using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class VersionController : Controller
    {
        /// <summary>
        /// This method returns the version of WhisperAPI
        /// </summary>
        /// <returns>ApiVersion of Whisper</returns>
        [HttpGet]
        public IActionResult GetVersion()
        {
            return this.Ok(new ApiVersion());
        }
    }
}