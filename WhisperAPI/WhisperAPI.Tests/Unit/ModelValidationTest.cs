using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using WhisperAPI.Controllers;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Context;
using WhisperAPI.Tests.Data.Builders;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class ModelValidationTest
    {
        private readonly ContextController _contextController;

        public ModelValidationTest()
        {
            var contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
            this._contextController = new ContextController(contexts);
        }

        [Test]
        public void When_max_documents_is_negative_return_model_state_invalid()
        {
            var query = SuggestionQueryBuilder.Build.WithMaxDocuments(-2).Instance;
            this.ValidateModel(query).Should().BeFalse();
        }

        [Test]
        public void When_max_documents_is_positive_return_model_state_valid()
        {
            var query = SuggestionQueryBuilder.Build.WithMaxDocuments(5).Instance;
            this.ValidateModel(query).Should().BeTrue();
        }

        [Test]
        public void When_max_questions_is_negative_return_model_state_invalid()
        {
            var query = SuggestionQueryBuilder.Build.WithMaxQuestions(-2).Instance;
            this.ValidateModel(query).Should().BeFalse();
        }

        [Test]
        public void When_max_questions_is_positive_return_model_state_valid()
        {
            var query = SuggestionQueryBuilder.Build.WithMaxQuestions(5).Instance;
            this.ValidateModel(query).Should().BeTrue();
        }

        [Test]
        [TestCase(5, -1)]
        [TestCase(-5, 1)]
        [TestCase(-5, -1)]
        public void When_max_questions_or_max_documents_is_negative_return_model_state_invalid(int maxDocuments, int maxQuestions)
        {
            var query = SuggestionQueryBuilder.Build.WithMaxDocuments(maxDocuments).WithMaxQuestions(maxQuestions).Instance;
            this.ValidateModel(query).Should().BeFalse();
        }

        [Test]
        [TestCase(5, 1)]
        [TestCase(0, 1)]
        [TestCase(5, 0)]
        public void When_max_questions_and_max_documents_are_positive_return_model_state_valid(int maxDocuments, int maxQuestions)
        {
            var query = SuggestionQueryBuilder.Build.WithMaxDocuments(maxDocuments).WithMaxQuestions(maxQuestions).Instance;
            this.ValidateModel(query).Should().BeTrue();
        }

        private bool ValidateModel(SuggestionQuery query)
        {
            var context = new ValidationContext(query, null, null);
            return Validator.TryValidateObject(query, context, null, true);
        }
    }
}
