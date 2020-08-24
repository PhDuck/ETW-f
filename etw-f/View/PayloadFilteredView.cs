namespace etw_f.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class PayloadFilteredView : IEquatable<PayloadFilteredView>
    {
        // DEVNOTE: If need be, change this to HashSet
        private readonly String[] _fields;

        internal IReadOnlyList<String> Fields => this._fields;

        internal PayloadFilteredView(String[] fields)
        {
            this._fields = fields;
        }

        internal Boolean IsSelected(String field)
        {
            return this._fields.Contains(field);
        }

        public Boolean Equals(PayloadFilteredView? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return this._fields.Length == other._fields.Length && this._fields.SequenceEqual(other._fields);
        }

        public override Boolean Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && this.Equals((PayloadFilteredView) obj);
        }

        public override Int32 GetHashCode()
        {
            unchecked
            {
                Int32 hash = 19;
                foreach (var foo in this._fields)
                {
                    hash = hash * 31 + foo.GetHashCode();
                }

                return hash;
            }
        }

        public static Boolean operator ==(PayloadFilteredView? left, PayloadFilteredView? right)
        {
            return Equals(left, right);
        }

        public static Boolean operator !=(PayloadFilteredView? left, PayloadFilteredView? right)
        {
            return !Equals(left, right);
        }
    }
}
