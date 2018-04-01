using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using WhisperAPI.Services;
using WhisperAPI.Models;

namespace WhisperAPI.ActionFilters
{
    public class ContextActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.RouteData.Values.Add("context", "\n\nbody\n\n");
            base.OnActionExecuting(context);
        }
    }
}
