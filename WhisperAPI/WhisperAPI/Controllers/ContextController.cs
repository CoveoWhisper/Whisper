using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using WhisperAPI.Models;
using WhisperAPI.Services;

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
            if (!this.ModelState.IsValid)
            {
                actionExecutingContext.Result = this.BadRequest(this.ModelState);
                return;
            }

            Guid chatKey;
            object param;

            if (actionExecutingContext.ActionArguments.TryGetValue("searchQuery", out param))
            {
                var searchQuery = (SearchQuery) param;
                chatKey = searchQuery.ChatKey.Value;
                log4net.ThreadContext.Properties["requestId"] = Guid.NewGuid();
                Log.Debug($"Search query:\r\n {JsonConvert.SerializeObject(searchQuery, Formatting.Indented)}");
            }
            else if (actionExecutingContext.ActionArguments.TryGetValue("chatkey", out param))
            {
                chatKey = (Guid) param;
                Log.Debug($"chatkey:\r\n {chatKey}");
            }
            else
            {
                Log.Error($"Unable to retrieve chatkey with action arguments: \r\n  {JsonConvert.SerializeObject(actionExecutingContext.ActionArguments, Formatting.Indented)}");
                return;
            }

            this.ConversationContext = this._contexts[chatKey];
            base.OnActionExecuting(actionExecutingContext);
        }
    }
}
