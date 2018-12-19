using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using WhisperAPI.Models;

namespace WhisperAPI.Tests.Data.Builders
{
    public class FacetQuestionBuilder
    {
        private string _facetName;

        private List<string> _facetValues;

        private Guid _id;

        private QuestionStatus _status;

        private string _answer;

        public static FacetQuestionBuilder Build => new FacetQuestionBuilder();

        public FacetQuestion Instance => new FacetQuestion
        {
            FacetName = this._facetName,
            FacetValues = this._facetValues,
            Answer = this._answer,
            Status = this._status,
            Id = this._id,
        };

        private FacetQuestionBuilder()
        {
            this._facetName = string.Empty;
            this._facetValues = new List<string>();
            this._id = Guid.NewGuid();
            this._status = QuestionStatus.None;
            this._answer = string.Empty;
        }

        public FacetQuestionBuilder WithFacetName(string facetName)
        {
            this._facetName = facetName;
            return this;
        }

        public FacetQuestionBuilder WithFacetValues(List<string> facetValues)
        {
            this._facetValues = facetValues;
            return this;
        }

        public FacetQuestionBuilder WithFacetValues(params string[] facetValues)
        {
            return this.WithFacetValues(facetValues.ToList());
        }

        public FacetQuestionBuilder WithId(Guid id)
        {
            this._id = id;
            return this;
        }

        public FacetQuestionBuilder WithStatus(QuestionStatus status)
        {
            this._status = status;
            return this;
        }

        public FacetQuestionBuilder WithAnswer(string answer)
        {
            this._answer = answer;
            return this;
        }
    }
}
