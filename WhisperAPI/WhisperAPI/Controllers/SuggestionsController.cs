using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;
using WhisperAPI.Services;
using WhisperAPI.ActionFilters;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class SuggestionsController : Controller
    {
        private readonly ISuggestionsService _suggestionsService;

        public SuggestionsController(ISuggestionsService suggestionsService)
        {
            this._suggestionsService = suggestionsService;
        }

        [ContextActionFilter]
        [HttpPost]
        public IActionResult GetSuggestions([FromBody] SearchQuerry searchQuerry)
        {
            if (searchQuerry?.ChatKey == null || searchQuerry?.Querry == null)
            {
                return this.BadRequest();
            }

            var t = RouteData.Values["context"];
            System.Diagnostics.Debug.WriteLine(t);

            return this.Ok(this._suggestionsService.GetSuggestions(searchQuerry.Querry).ToList());
        }
    }
}
