using System;
using System.Collections.Generic;
using System.Text;
using WhisperAPI.Models;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class FacetQuestionResultBuilder
    {
        private FacetQuestion _facetQuestion;

        private double _score;

        public static FacetQuestionResultBuilder Build => new FacetQuestionResultBuilder();

        public FacetQuestionResult Instance => new FacetQuestionResult
        {
            FacetQuestion = this._facetQuestion,
            Score = this._score
        };

        private FacetQuestionResultBuilder()
        {
            this._facetQuestion = FacetQuestionBuilder.Build.Instance;
            this._score = 0.75;
        }

        public FacetQuestionResultBuilder WithFacetQuestion(FacetQuestion facetQuestion)
        {
            this._facetQuestion = facetQuestion;
            return this;
        }

        public FacetQuestionResultBuilder WithScore(double score)
        {
            this._score = score;
            return this;
        }
    }
}
