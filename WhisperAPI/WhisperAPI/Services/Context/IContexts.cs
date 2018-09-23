using System;
using WhisperAPI.Models;

namespace WhisperAPI.Services.Context
{
    public interface IContexts
    {
        ConversationContext this[Guid chatkey] { get; }
    }
}
