using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class FacetValuesBuilder
    {
        private string _name;

        private List<string> _values;

        public static FacetValuesBuilder Build => new FacetValuesBuilder();

        public FacetValues Instance => new FacetValues
        {
            Name = this._name,
            Values = this._values
        };

        private FacetValuesBuilder()
        {
            this._name = "name";
            this._values = new List<string> { "values" };
        }

        public FacetValuesBuilder WithName(string name)
        {
            this._name = name;
            return this;
        }

        public FacetValuesBuilder WithValues(List<string> values)
        {
            this._values = values;
            return this;
        }

        public FacetValuesBuilder AddValue(string value)
        {
            this._values.Add(value);
            return this;
        }
    }
}
