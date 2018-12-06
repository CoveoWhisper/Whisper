using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using WhisperAPI.Models.Queries;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace WhisperAPI.Tests.Unit
{
    [TestFixture]
    public class SearchQueryTest
    {
        [Test]
        public void When_sending_message_type_null_then_modelstate_is_not_valid()
        {
            var jsonSearchQuery = "{\"chatkey\": \"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Query\": \"Need help with CoveoSearch API\"}";

            var searchQuery = JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery);
            var context = new ValidationContext(searchQuery, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(searchQuery, context, result, true);

            valid.Should().BeFalse();

            result.Select(x => x.ErrorMessage).FirstOrDefault().Should().BeEquivalentTo("Type is required");
        }

        [Test]
        public void When_sending_chatKey_null_then_modelstate_is_not_valid()
        {
            var jsonSearchQuery = "{\"Type\": 1,\"Query\": \"Need help with CoveoSearch API\"}";

            var searchQuery = JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery);
            var context = new ValidationContext(searchQuery, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(searchQuery, context, result, true);

            valid.Should().BeFalse();
            result.Select(x => x.ErrorMessage).FirstOrDefault().Should().BeEquivalentTo("ChatKey is required");
        }

        [Test]
        public void When_sending_good_model_then_modelstate_is_valid()
        {
            var jsonSearchQuery = "{\"chatkey\": \"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Type\": 1,\"Query\": \"Need help with CoveoSearch API\", \"aq\": \"(@test==HelloIamatest) \"}";

            var searchQuery = JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery);
            var context = new ValidationContext(searchQuery, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(searchQuery, context, result, true);

            valid.Should().BeTrue();
        }

        [Test]
        public void When_sending_optionnal_overriden_recommender_settings_null_then_modelstate_is_valid()
        {
            var jsonSearchQuery = "{\"chatkey\": \"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Type\": 1,\"Query\": \"Need help with CoveoSearch API\", \"aq\": \"(@test==HelloIamatest) \"}";

            var searchQuery = JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery);
            var context = new ValidationContext(searchQuery, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(searchQuery, context, result, true);

            valid.Should().BeTrue();

            searchQuery.OverridenRecommenderSettings.Should().BeNull();
        }

        [Test]
        public void When_sending_optionnal_overriden_recommender_settings_value_then_modelstate_is_valid()
        {
            var jsonSearchQuery = "{\"chatkey\":\"aecaa8db-abc8-4ac9-aa8d-87987da2dbb0\",\"Type\":1,\"Query\":\"Need help with CoveoSearch API\",\"aq\":\"(@test==HelloIamatest) \",\"overridenRecommenderSettings\":{\"useLongQuerySearchRecommender\":false,\"usePreprocessedQuerySearchRecommender\":false,\"useAnalyticsSearchRecommender\":true,\"useFacetQuestionRecommender\":true}}";

            var searchQuery = JsonConvert.DeserializeObject<SearchQuery>(jsonSearchQuery);
            var context = new ValidationContext(searchQuery, null, null);
            var result = new List<ValidationResult>();

            var valid = Validator.TryValidateObject(searchQuery, context, result, true);

            valid.Should().BeTrue();

            searchQuery.OverridenRecommenderSettings.UseLongQuerySearchRecommender.Should().BeFalse();
            searchQuery.OverridenRecommenderSettings.UsePreprocessedQuerySearchRecommender.Should().BeFalse();
            searchQuery.OverridenRecommenderSettings.UseAnalyticsSearchRecommender.Should().BeTrue();
            searchQuery.OverridenRecommenderSettings.UseFacetQuestionRecommender.Should().BeTrue();
        }
    }
}
