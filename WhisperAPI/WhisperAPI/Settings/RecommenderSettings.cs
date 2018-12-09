namespace WhisperAPI.Settings
{
    public class RecommenderSettings
    {
        public bool UseLongQuerySearchRecommender { get; set; }

        public bool UsePreprocessedQuerySearchRecommender { get; set; }

        public bool UseAnalyticsSearchRecommender { get; set; }

        public bool UseFacetQuestionRecommender { get; set; }

        public bool UseNearestDocumentsRecommender { get; set; }
    }
}
