using WhisperAPI.Models;

namespace WhisperAPI.Tests.Data.Builders
{
    public class QuestionBuilder
    {
        private string _text;

        public static QuestionBuilder Build => new QuestionBuilder();

        public Question Instance => new Question
        {
            Text = this._text
        };

        private QuestionBuilder()
        {
            this._text = "Text";
        }

        public QuestionBuilder WithText(string text)
        {
            this._text = text;
            return this;
        }
    }
}
