using System;
using System.Collections.Generic;
using WhisperAPI.Models.MLAPI;

namespace WhisperAPI.Tests.Data.Builders
{
    public class FacetBuilder
    {
        private Guid _id;

        private string _name;

        private List<string> _values;

        public static FacetBuilder Build => new FacetBuilder();

        public Facet Instance => new Facet
        {
            Id = this._id,
            Name = this._name,
            Values = this._values
        };

        private FacetBuilder()
        {
            this._id = Guid.NewGuid();
            this._name = "name";
            this._values = new List<string>();
        }

        public FacetBuilder WithName(string name)
        {
            this._name = name;
            return this;
        }

        public FacetBuilder WithValues(List<string> values)
        {
            this._values = values;
            return this;
        }

        public FacetBuilder WithId(Guid id)
        {
            this._id = id;
            return this;
        }

        public FacetBuilder AddValue(string value)
        {
            this._values.Add(value);
            return this;
        }
    }
}
