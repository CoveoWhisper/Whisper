using System.Collections.Generic;
using WhisperAPI.Models;

namespace WhisperAPI.Tests.Data.Builders
{
    public class RecommendationBuilder<T>
    {
        private T _value;

        private double _confidence;

        private List<RecommenderType> _recommendedBy;

        public static RecommendationBuilder<T> Build => new RecommendationBuilder<T>();

        public Recommendation<T> Instance => new Recommendation<T>
        {
            Value = this._value,
            Confidence = this._confidence,
            RecommendedBy = this._recommendedBy
        };

        private RecommendationBuilder()
        {
            this._confidence = 1;
            this._recommendedBy = new List<RecommenderType>
            {
                RecommenderType.LongQuerySearch
            };
        }

        public RecommendationBuilder<T> WithValue(T value)
        {
            this._value = value;
            return this;
        }

        public RecommendationBuilder<T> WithConfidence(double confidence)
        {
            this._confidence = confidence;
            return this;
        }

        public RecommendationBuilder<T> WithRecommendationType(List<RecommenderType> recommendedBy)
        {
            this._recommendedBy = recommendedBy;
            return this;
        }

        public RecommendationBuilder<T> AddRecommendationType(RecommenderType recommendedBy)
        {
            this._recommendedBy.Add(recommendedBy);
            return this;
        }
    }
}
