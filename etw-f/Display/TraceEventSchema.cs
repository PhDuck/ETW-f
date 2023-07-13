namespace etw_f.Display
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.Diagnostics.Tracing;

    internal static class TraceEventSchema
    {
        private const String HeaderFormatString = "{0,-25:u}{1,-25} {2,-20}{3,-0}";
        private static readonly String[] DefaultHeaders = { "Timestamp", "Provider", "Event Name", "Payload" };
        
        /// <summary>
        /// Writes the default header for a trace event.
        /// </summary>
        /// <param name="writer"></param>
        internal static void WriteHeaders(TextWriter writer)
        {
            // ReSharper disable once CoVariantArrayConversion
            writer.WriteLine(HeaderFormatString, DefaultHeaders);
        }

        internal static void AppendHeaderValues(StringBuilder sb, TraceEvent te)
        {
            sb.AppendFormat(HeaderFormatString, te.TimeStamp.ToUniversalTime(), te.ProviderName, te.EventName, "");
        }
    }
}