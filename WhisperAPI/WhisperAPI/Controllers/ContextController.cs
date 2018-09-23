using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using WhisperAPI.Models;
using WhisperAPI.Services.Context;

namespace WhisperAPI.Controllers
{
    public class ContextController : Controller
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IContexts _contexts;

        public ContextController(IContexts contexts)
        {
            this._contexts = contexts;
        }

        protected ConversationContext ConversationContext { get; private set; }

        public override void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            var searchQuery = (SearchQuery)actionExecutingContext.ActionArguments["searchQuery"];

            log4net.ThreadContext.Properties["requestId"] = Guid.NewGuid();
            if (!this.ModelState.IsValid)
            {
                actionExecutingContext.Result = this.BadRequest(this.ModelState);
                Log.Error($"Search query:\r\n{JsonConvert.SerializeObject(searchQuery, Formatting.Indented)}");
                return;
            }

            Log.Debug($"Search query:\r\n {JsonConvert.SerializeObject(searchQuery, Formatting.Indented)}");
            Guid chatKey = searchQuery.ChatKey.Value;
            this.ConversationContext = this._contexts[chatKey];

            base.OnActionExecuting(actionExecutingContext);
        }
    }
}
