using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Services.Context;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class ContextsTest
    {
        private readonly InMemoryContexts _contexts;

        public ContextsTest()
        {
            this._contexts = new InMemoryContexts(new TimeSpan(1, 0, 0, 0));
        }

        [Test]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_adding_duplicate_suggestion_only_one_is_added(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            SuggestedDocument query1 = this.GetSuggestedDocument();
            SuggestedDocument query2 = this.GetSuggestedDocument();
            conversationcontext.SuggestedDocuments.Add(query1);
            conversationcontext.SuggestedDocuments.Add(query2);
            conversationcontext.SuggestedDocuments.Should().HaveCount(1);
        }

        [Test]
        [Order(0)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_retrieving_non_existing_conversation_context_then_return_new_one(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SuggestedDocuments.Add(this.GetSuggestedDocument());

            conversationcontext.Should().NotBeNull();
            conversationcontext.ChatKey.Should().Be(chatkey);
        }

        [Test]
        [Order(1)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_retrieving_existing_conversation_context_then_return_the_context(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];

            conversationcontext.SuggestedDocuments.Count.Should().Be(1);
            conversationcontext.SuggestedDocuments.Should().Contain(this.GetSuggestedDocument());
            conversationcontext.ChatKey.Should().Be(chatkey);
        }

        [Test]
        [Order(2)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_life_span_is_not_expired_then_context_is_not_deleted(string chatkey)
        {
            IEnumerable<ConversationContext> removedContext = this._contexts.RemoveOldContext();

            removedContext.Should().BeEmpty();
        }

        [Test]
        [Order(3)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_life_span_expired_then_context_is_deleted(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.StartDate = conversationcontext.StartDate.Subtract(new TimeSpan(2, 0, 0, 0));

            IEnumerable<ConversationContext> removedContext = this._contexts.RemoveOldContext();

            removedContext.Should().OnlyContain(x => x.Equals(conversationcontext));
        }

        [Test]
        [Order(4)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_adding_received_message_to_context_messages_concats_and_persists(string chatkey)
        {
            ConversationContext conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SearchQueries.Add(this.GetSearchQuery("rest api", chatkey));

            conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SearchQueries[0].Query.Should().Be("rest api");

            conversationcontext.SearchQueries.Add(this.GetSearchQuery("framework", chatkey));

            conversationcontext = this._contexts[new Guid(chatkey)];
            conversationcontext.SearchQueries[0].Query.Should().Be("rest api");
            conversationcontext.SearchQueries[1].Query.Should().Be("framework");
        }

        private SearchQuery GetSearchQuery(string query, string chatkey)
        {
            return new SearchQuery
            {
                ChatKey = new Guid(chatkey),
                Query = query,
                Type = SearchQuery.MessageType.Customer
            };
        }

        private SuggestedDocument GetSuggestedDocument()
        {
            return new SuggestedDocument()
            {
                Title = "title",
                PrintableUri = "www.test.com",
                Uri = "www.test.com",
                Summary = "this is a summary"
            };
        }
    }
}
