using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;
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
        public void When_adding_duplicate_suggestion_only_one_is_added(string chatKey)
        {
            ConversationContext conversationContext = this._contexts[new Guid(chatKey)];
            Document query1 = GetDocument();
            Document query2 = GetDocument();
            conversationContext.SuggestedDocuments.Add(query1);
            conversationContext.SuggestedDocuments.Add(query2);
            conversationContext.SuggestedDocuments.Should().HaveCount(1);
        }

        [Test]
        [Order(0)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_retrieving_non_existing_conversation_context_then_return_new_one(string chatKey)
        {
            ConversationContext conversationContext = this._contexts[new Guid(chatKey)];
            conversationContext.SuggestedDocuments.Add(GetDocument());

            conversationContext.Should().NotBeNull();
            conversationContext.ChatKey.Should().Be(chatKey);
        }

        [Test]
        [Order(1)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_retrieving_existing_conversation_context_then_return_the_context(string chatKey)
        {
            ConversationContext conversationContext = this._contexts[new Guid(chatKey)];

            conversationContext.SuggestedDocuments.Count.Should().Be(1);
            conversationContext.SuggestedDocuments.Should().Contain(GetDocument());
            conversationContext.ChatKey.Should().Be(chatKey);
        }

        [Test]
        [Order(2)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289502")]
        public void When_life_span_is_not_expired_then_context_is_not_deleted(string chatKey)
        {
            IEnumerable<ConversationContext> removedContext = this._contexts.RemoveOldContext();

            removedContext.Should().BeEmpty();
        }

        [Test]
        [Order(3)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_life_span_expired_then_context_is_deleted(string chatKey)
        {
            ConversationContext conversationContext = this._contexts[new Guid(chatKey)];
            conversationContext.StartDate = conversationContext.StartDate.Subtract(new TimeSpan(2, 0, 0, 0));

            IEnumerable<ConversationContext> removedContext = this._contexts.RemoveOldContext();

            removedContext.Should().OnlyContain(x => x.Equals(conversationContext));
        }

        [Test]
        [Order(4)]
        [TestCase("0f8fad5b-d9cb-469f-a165-708677289501")]
        public void When_adding_received_message_to_context_messages_concats_and_persists(string chatKey)
        {
            ConversationContext conversationContext = this._contexts[new Guid(chatKey)];
            conversationContext.ContextItems.Add(GetContextItem("rest api", chatKey));

            conversationContext = this._contexts[new Guid(chatKey)];
            conversationContext.ContextItems[0].SearchQuery.Query.Should().Be("rest api");

            conversationContext.ContextItems.Add(GetContextItem("framework", chatKey));

            conversationContext = this._contexts[new Guid(chatKey)];
            conversationContext.ContextItems[0].SearchQuery.Query.Should().Be("rest api");
            conversationContext.ContextItems[1].SearchQuery.Query.Should().Be("framework");
        }

        private static ContextItem GetContextItem(string query, string chatKey)
        {
            return new ContextItem
            {
                SearchQuery = new SearchQuery
                {
                    ChatKey = new Guid(chatKey),
                    Query = query,
                    Type = SearchQuery.MessageType.Customer
                }
            };
        }

        private static Document GetDocument()
        {
            return new Document()
            {
                Title = "title",
                PrintableUri = "www.test.com",
                Uri = "www.test.com",
                Summary = "this is a summary"
            };
        }
    }
}
