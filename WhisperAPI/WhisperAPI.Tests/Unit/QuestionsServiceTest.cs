using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using WhisperAPI.Models;
using WhisperAPI.Models.Queries;
using WhisperAPI.Services.Questions;
using WhisperAPI.Tests.Data.Builders;
using static WhisperAPI.Models.Queries.SearchQuery;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class QuestionsServiceTest
    {
        private IQuestionsService _questionsService;

        private ConversationContext _conversationContext;

        [SetUp]
        public void SetUp()
        {
            this._questionsService = new QuestionsService();
            this._conversationContext = new ConversationContext(new Guid("a21d07d5-fd5a-42ab-ac2c-2ef6101e58d9"), DateTime.Now);
        }

        [Test]
        [TestCase]
        public void When_receive_agent_message_then_no_answer_detected()
        {
            var agentQuery = SearchQueryBuilder.Build.WithMessageType(SearchQuery.MessageType.Agent).Instance;
            Assert.IsFalse(this._questionsService.DetectAnswer(null, agentQuery));
        }

        [TestCase(true, "Asparagus", "I love asparagus", "food", new string[] { "Asparagus", "Brocoli" })]
        [TestCase(true, "As paragus", "I love as paragus", "food", new string[] { "As paragus", "Brocoli" })]
        [TestCase(true, "Asparagus", "I love as paragus", "food", new string[] { "Asparagus", "Brocoli" })]
        [TestCase(true, "asparagus", "I love asparagus", "food", new string[] { "asparagus", "Brocoli" })]
        [TestCase(false, "", "I love Cantelope", "food", new string[] { "Asparagus", "Brocoli" })]
        public void When_receive_customer_message_with_answer_to_pending_question_then_answer_detected(bool shouldDetectAnswer, string expectedAnswer, string custommerMessage, string facetName, string[] facetValues)
        {
            var pendingQuestion = FacetQuestionBuilder.Build
                .WithStatus(QuestionStatus.AnswerPending)
                .WithFacetName(facetName)
                .WithFacetValues(facetValues)
                .Instance;

            this._conversationContext.Questions.Add(pendingQuestion);
            var questionAnswer = SearchQueryBuilder.Build
                .WithMessageType(MessageType.Customer)
                .WithQuery(custommerMessage)
                .Instance;

            Assert.AreEqual(shouldDetectAnswer, this._questionsService.DetectAnswer(this._conversationContext, questionAnswer));
            if (shouldDetectAnswer)
            {
                Assert.AreEqual(expectedAnswer, ((FacetQuestion)this._conversationContext.AnsweredQuestions.Single()).Answer);
                this._conversationContext.MustHaveFacets.Should().HaveCount(1);
            }
            else
            {
                this._conversationContext.MustHaveFacets.Should().HaveCount(0);
            }
        }

        [TestCase(true, "My question is: Is year 2018 ?", "Year", new string[] { "2018", "2017" })]
        [TestCase(true, "My question is: Is year 2018 ?", "Year", new string[] { "20 18", "2017" })]
        [TestCase(true, "My question is: Is year 20 18 ?", "Year", new string[] { "20 18", "2017" })]
        [TestCase(true, "My question is: Is it Asparagus or Brocoli ?", "Dummy", new string[] { "Asparagus", "Brocoli" })]
        [TestCase(true, "My question is: Is it Asparagus or Brocoli ?", "Dummy", new string[] { "asparagus", "brocoli" })]
        [TestCase(true, "My question is: Is Dummy Asparagus or Brocoli ?", "Dummy", new string[] { "Asparagus", "Cantelope" })]
        [TestCase(false, "My question is: Is it A or B ?", "Dummy", new string[] { "C", "D" })]
        [TestCase(false, "My question is: Is Dummy Asparagus or Brocoli ?", "Dummy", new string[] { "Cantelope", "Databurger" })]
        [TestCase(false, "My question is: Is it A or B ?", "Dummy", new string[] { "A", "C" })]
        [TestCase(false, "My cat is stuck in a tree.", "Dummy", new string[] { "Asparagus", "Cantelope" })]
        public void When_receive_question_that_was_clicked_in_message_then_detect_question_and_set_to_pending_questionIfShouldDetect(bool shouldDetect, string agentMessage, string facetName, string[] facetValues)
        {
            var clickedQuestion = FacetQuestionBuilder.Build
                .WithStatus(QuestionStatus.Clicked)
                .WithFacetName(facetName)
                .WithFacetValues(facetValues)
                .Instance;

            this._conversationContext.Questions.Add(clickedQuestion);
            var questionAsked = SearchQueryBuilder.Build
                .WithMessageType(MessageType.Agent)
                .WithQuery(agentMessage)
                .Instance;

            Assert.AreEqual(shouldDetect, this._questionsService.DetectQuestionAsked(this._conversationContext, questionAsked));
            Assert.AreEqual(shouldDetect, clickedQuestion.Status == QuestionStatus.AnswerPending);
        }

        [TestCase]
        public void When_receive_question_that_was_cancelled_set_to_rejected_question()
        {
            var questionToRejected = FacetQuestionBuilder.Build
                .WithStatus(QuestionStatus.Answered)
                .WithFacetName("Dummy")
                .WithFacetValues("A", "B")
                .Instance;

            var facet = FacetBuilder.Build
                .WithId(questionToRejected.Id)
                .AddValue(questionToRejected.Answer)
                .WithName(questionToRejected.FacetName)
                .Instance;

            this._conversationContext.MustHaveFacets.Add(facet);
            this._conversationContext.Questions.Add(questionToRejected);

            Assert.IsTrue(this._questionsService.RejectAnswer(this._conversationContext, questionToRejected.Id));
            Assert.AreEqual(QuestionStatus.Rejected, questionToRejected.Status);
            this._conversationContext.MustHaveFacets.Should().HaveCount(0);
        }
    }
}
