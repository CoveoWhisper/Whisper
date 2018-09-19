using System;

namespace WhisperAPI.Models
{
    public class Question
    {
        public Guid Id { get; set; }

        public string Text { get; set; }

        public override bool Equals(object obj)
        {
            var question = obj as Question;
            return question != null &&
                   Id.Equals(question.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}
