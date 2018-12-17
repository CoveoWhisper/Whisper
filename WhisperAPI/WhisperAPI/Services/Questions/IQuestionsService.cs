using System;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;

namespace WhisperAPI.Services.Questions
{
    public interface IQuestionsService
    {
        /// <summary>
        /// Detect if a question has been asked if the conversation
        /// </summary>
        /// <param name="context">Context of the conversation</param>
        /// <param name="message">Last message from conversation</param>
        /// <returns>True if detected, false if not</returns>
        bool DetectQuestionAsked(ConversationContext context, SearchQuery message);

        /// <summary>
        /// Detect an aswer to a question asked
        /// </summary>
        /// <param name="context">Context of the conversation</param>
        /// <param name="message">Last message from conversation</param>
        /// <returns>True if detected, false if not</returns>
        bool DetectAnswer(ConversationContext context, SearchQuery message);

        /// <summary>
        /// Reject an answer and change the status of the question to rejected
        /// </summary>
        /// <param name="context">Context of the conversation</param>
        /// <param name="questionId">QuestionId to be rejected</param>
        /// <returns>True if done, false if not</returns>
        bool RejectAnswer(ConversationContext context, Guid questionId);

        /// <summary>
        /// Reject all answer and change the status of all question to rejected
        /// </summary>
        /// <param name="context">Context of the conversation</param>
        void RejectAllAnswers(ConversationContext context);
    }
}