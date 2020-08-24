namespace etw_f.Filtering
{
    using System;

    using Microsoft.Diagnostics.Tracing;

    internal class PayloadFieldBoundFilterExpression : FilterExpression, IEquatable<PayloadFieldBoundFilterExpression>
    {
        public readonly String FieldName;
        private Int32? _fieldIndex;

        internal PayloadFieldBoundFilterExpression(String pattern, String fieldName)
            : base(pattern, FilterType.Payload)
        {
            // TODO: Verify that the field actually exists.
            this.FieldName = fieldName;
        }

        internal override Boolean Evaluate(TraceEvent te)
        {
            this._fieldIndex ??= te.PayloadIndex(this.FieldName);

            String payloadString = te.PayloadString(this._fieldIndex.Value);

            return payloadString != null && this._regex.IsMatch(payloadString);
        }

        public override Boolean Equals(Object? obj)
        {
            return this.Equals(obj as PayloadFieldBoundFilterExpression);
        }

        public Boolean Equals(PayloadFieldBoundFilterExpression? fe)
        {
            return fe != null && fe.FieldName == this.FieldName && base.Equals(fe);
        }

        public override Int32 GetHashCode()
        {
            return HashCode.Combine(this.FieldName.GetHashCode(), base.GetHashCode());
        }
    }
}

