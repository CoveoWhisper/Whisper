﻿using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using WhisperAPI.Controllers;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Context;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    class ModelValidationTest
    {
        private readonly InMemoryContexts _contexts;
        private readonly ContextController _contexyController;

        public ModelValidationTest()
        {
            this._contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
            this._contexyController = new ContextController(this._contexts);
        }

        [Test]
        public void When_max_documents_is_negative_return_model_state_invalid()
        {
            var query = SuggestionQueryBuilder.Build.WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289502")).WithMaxDocuments(-2).Instance;
            this.ValidateModel(query).Should().BeFalse();
        }

        [Test]
        public void When_max_documents_is_positive_return_model_state_valid()
        {
            var query = SuggestionQueryBuilder.Build.WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289502")).WithMaxDocuments(5).Instance;
            this.ValidateModel(query).Should().BeTrue();
        }

        [Test]
        public void When_max_questions_is_negative_return_model_state_invalid()
        {
            var query = SuggestionQueryBuilder.Build.WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289502")).WithMaxQuestions(-2).Instance;
            this.ValidateModel(query).Should().BeFalse();
        }

        [Test]
        public void When_max_questions_is_positive_return_model_state_valid()
        {
            var query = SuggestionQueryBuilder.Build.WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289502")).WithMaxQuestions(5).Instance;
            this.ValidateModel(query).Should().BeTrue();
        }

        [Test]
        [TestCase(5, -1)]
        [TestCase(-5, 1)]
        [TestCase(-5, -1)]
        public void When_max_questions_or_max_documents_is_negative_return_model_state_invalid(int maxDocuments, int maxQuestions)
        {
            var query = SuggestionQueryBuilder.Build.WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289502")).WithMaxDocuments(maxDocuments).WithMaxQuestions(maxQuestions).Instance;
            this.ValidateModel(query).Should().BeFalse();
        }

        [Test]
        [TestCase(5, 1)]
        [TestCase(0, 1)]
        [TestCase(5, 0)]
        public void When_max_questions_and_max_documents_are_positive_return_model_state_valid(int maxDocuments, int maxQuestions)
        {
            var query = SuggestionQueryBuilder.Build.WithChatKey(new Guid("0f8fad5b-d9cb-469f-a165-708677289502")).WithMaxDocuments(maxDocuments).WithMaxQuestions(maxQuestions).Instance;
            this.ValidateModel(query).Should().BeTrue();
        }

        private bool ValidateModel(SuggestionQuery query)
        {
            var actionContext = new ActionContext(
                new Mock<HttpContext>().Object,
                new Mock<RouteData>().Object,
                new Mock<ActionDescriptor>().Object);

            var actionExecutingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                this._contexyController);
            actionExecutingContext.ActionArguments["query"] = query;
            if (query != null)
            {
                var context = new ValidationContext(query, null, null);
                var results = new List<ValidationResult>();

                if (!Validator.TryValidateObject(query, context, results, true))
                {
                    actionExecutingContext.ModelState.Clear();
                    this._contexyController.ModelState.Clear();
                    foreach (ValidationResult result in results)
                    {
                        var key = result.MemberNames.FirstOrDefault() ?? string.Empty;
                        actionExecutingContext.ModelState.AddModelError(key, result.ErrorMessage);
                        this._contexyController.ModelState.AddModelError(key, result.ErrorMessage);
                    }
                }

                return actionExecutingContext.ModelState.IsValid;
            }
            return false;
        }
    }
}
