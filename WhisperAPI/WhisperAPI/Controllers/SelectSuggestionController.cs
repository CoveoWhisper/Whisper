using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;
using WhisperAPI.Services;
using WhisperAPI.Services.Context;

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
        public IActionResult SelectSuggestion([FromBody] SelectSuggestionPost selectSuggestionPost)
        {
            if (!this.ModelState.IsValid || selectSuggestionPost?.Id == null)
            {
                return this.BadRequest();
            }

            this._selectSuggestionService.UpdateContextWithSelectedSuggestion(this.ConversationContext, selectSuggestionPost);
            Log.Debug($"Id: {selectSuggestionPost.Id}");

            return this.Ok();
        }
    }
}
