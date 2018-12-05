using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;

namespace WhisperAPI.Models
{
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    public enum RecommenderType
    {
        LongQuerySearch,
        PreprocessedQuerySearch,
        LastClickAnalytics,
        NearestDocuments,
        FacetQuestions
    }

    public class Recommendation<T>
    {
        public T Value { get; set; }

        /// <summary>
        /// Between 0 and 1
        /// </summary>
        public double Confidence { get; set; }

        public List<RecommenderType> RecommendedBy { get; set; }

        public Recommendation<T2> ConvertValue<T2>(Func<T, T2> project)
        {
            return new Recommendation<T2>
            {
                Value = project(this.Value),
                Confidence = this.Confidence,
                RecommendedBy = this.RecommendedBy
            };
        }
    }
}
