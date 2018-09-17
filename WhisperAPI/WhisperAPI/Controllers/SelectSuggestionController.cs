using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;
using WhisperAPI.Services.Context;
using WhisperAPI.Services.SelectSuggestion;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class SelectSuggestionController : ContextController
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISelectSuggestionService _selectSuggestionService;

        public SelectSuggestionController(ISelectSuggestionService selectSuggestionService, IContexts contexts)
            : base(contexts)
        {
            this._selectSuggestionService = selectSuggestionService;
        }

        [HttpPost]
        public IActionResult SelectSuggestion([FromBody] SearchQuery searchQuery)
        {
            if (!this.ModelState.IsValid || searchQuery?.Query == null)
            {
                return this.BadRequest();
            }

            bool isContextUpdated = this._selectSuggestionService.UpdateContextWithSelectedSuggestion(this.ConversationContext, searchQuery);
            if (!isContextUpdated)
            {
                return this.BadRequest();
            }

            Log.Debug($"Select suggestion with id {searchQuery.Query}");

            return this.Ok();
        }
    }
}
