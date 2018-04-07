﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WhisperAPI.Models;
using WhisperAPI.Services;
using static WhisperAPI.Models.SearchQuerry;

namespace WhisperAPI.Controllers
{
    [Route("/Whisper/[Controller]")]
    public class SuggestionsController : ContextController
    {
        private readonly ISuggestionsService _suggestionsService;

        public SuggestionsController(ISuggestionsService suggestionsService, Contexts contexts)
            : base(contexts)
        {
            this._suggestionsService = suggestionsService;
        }

        [HttpPost]
        public IActionResult GetSuggestions([FromBody] SearchQuerry searchQuerry)
        {
            if (!this.ModelState.IsValid || searchQuerry?.Querry == null)
            {
                return this.BadRequest();
            }

            return this.Ok(this._suggestionsService.GetSuggestions(searchQuerry.Querry).ToList());
        }
    }
}
