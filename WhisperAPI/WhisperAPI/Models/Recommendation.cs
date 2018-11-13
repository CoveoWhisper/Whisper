using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhisperAPI.Models
{
    public enum RecommenderType
    {
        LongQuerySearch,
        PreprocessedQuerySearch,
        LastClickAnalytics,
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
