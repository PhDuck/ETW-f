namespace etw_f.Filtering
{
    using System;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    using Microsoft.Diagnostics.Tracing;

    internal class FilterExpression : IEquatable<FilterExpression>
    {
        protected readonly String pattern;
        protected readonly Regex _regex;
        protected readonly FilterType _type;

        protected FilterExpression(String pattern, FilterType type)
        {
            this.pattern = pattern;
            this._type = type;
            this._regex = new Regex(pattern, RegexOptions.Compiled);
        }

        internal static FilterExpression Create(String filter)
        {
            return new FilterExpression(filter, FilterType.Wildcard);
        }

        internal virtual Boolean Evaluate(TraceEvent te)
        {
            switch (this._type)
            {
                case FilterType.Wildcard:
                    return this._regex.IsMatch(te.Dump());
                case FilterType.Payload:
                    Debug.Fail("Should not hit");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
        }

        public override Boolean Equals(Object? obj)
        {
            return this.Equals(obj as FilterExpression);
        }

        public Boolean Equals(FilterExpression? fe)
        {
            return fe != null && this.pattern.Equals(fe.pattern) && this._type.Equals(fe._type);
        }

        public override Int32 GetHashCode()
        {
            return HashCode.Combine(this.pattern.GetHashCode(), this._type.GetHashCode());
        }
    }
}