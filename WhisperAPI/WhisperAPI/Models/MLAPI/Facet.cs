﻿namespace WhisperAPI.Models.MLAPI
{
    public class Facet
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((Facet)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Name != null ? this.Name.GetHashCode() : 0) * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
            }
        }

        protected bool Equals(Facet other)
        {
            return string.Equals(this.Name, other.Name) && string.Equals(this.Value, other.Value);
        }
    }
}
