using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WhisperAPI.Models;

namespace WhisperAPI.Services.Context
{
    public class Contexts : DbContext
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private object _removeOldLock = new object();

        public Contexts(DbContextOptions<Contexts> options, TimeSpan contextLifeSpan)
            : base(options)
        {
            this.ContextLifeSpan = contextLifeSpan;
        }

        public TimeSpan ContextLifeSpan { get; set; }

        private DbSet<ConversationContext> ConversationContexts { get; set; }

        // Get the Conversation associated to the chatkey,
        // create a new one if doesn't already exist
        public ConversationContext this[Guid chatkey]
        {
            get
            {
                ConversationContext conversationContext = this.ConversationContexts
                    .Include(x => x.SearchQueries)
                    .Include(x => x.SuggestedDocuments)
                    .FirstOrDefault(x => x.ChatKey == chatkey);

                if (conversationContext == null)
                {
                    conversationContext = new ConversationContext(chatkey, DateTime.Now);
                    this.ConversationContexts.Add(conversationContext);
                    this.SaveChangesAsync();
                }

                return conversationContext;
            }
        }

        public List<ConversationContext> RemoveOldContext()
        {
            lock (this._removeOldLock)
            {
                List<ConversationContext> removedContexts = this.ConversationContexts.Where(x => ((DateTime.Now - x.StartDate) > this.ContextLifeSpan)).ToList();

                foreach (var context in removedContexts)
                {
                    this.ConversationContexts.Remove(context);
                }

                this.SaveChangesAsync();

                return removedContexts;
            }
        }
    }
}
