using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WhisperAPI.Models;

namespace WhisperAPI.Services.Context
{
    public class InMemoryContexts : IContexts
    {
        public InMemoryContexts(TimeSpan contextLifeSpan)
        {
            this.ContextLifeSpan = contextLifeSpan;
            this.ConversationContexts = new ConcurrentDictionary<Guid, ConversationContext>();
        }

        public TimeSpan ContextLifeSpan { get; set; }

        private ConcurrentDictionary<Guid, ConversationContext> ConversationContexts { get; }

        public ConversationContext this[Guid chatKey]
        {
            get
            {
                if (!this.ConversationContexts.TryGetValue(chatKey, out ConversationContext conversationContext))
                {
                    this.RemoveOldContext();

                    conversationContext = new ConversationContext(chatKey, DateTime.Now);
                    this.ConversationContexts.TryAdd(chatKey, conversationContext);
                }

                return conversationContext;
            }
        }

        public List<ConversationContext> RemoveOldContext()
        {
            List<ConversationContext> removedContexts = this.ConversationContexts
                .Where(x => ((DateTime.Now - x.Value.StartDate) > this.ContextLifeSpan))
                .Select(x => x.Value).ToList();

            foreach (var context in removedContexts)
            {
                this.ConversationContexts.TryRemove(context.ChatKey, out ConversationContext value);
            }

            return removedContexts;
        }
    }
}
