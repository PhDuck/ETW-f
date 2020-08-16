namespace etw_f.Display
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Extensions.ObjectPool;

    /// <summary>
    /// Class that defines the basic way a trace event is displayed.
    /// </summary>
    internal class TraceDisplayer
    {
        internal static readonly TraceDisplayer Default = new TraceDisplayer();

        private static readonly ObjectPool<StringBuilder> SbPool = ObjectPool.Create<StringBuilder>();

        /// <summary>
        /// The default way to display a 
        /// </summary>
        /// <param name="te"></param>
        /// <param name="writer"></param>
        internal static void DefaultDisplay(TraceEvent te, TextWriter writer)
        {
            StringBuilder sb = SbPool.Get();
            try
            {
                TraceEventSchema.AppendHeaders(sb, te);
                sb.Append("{ ");
                foreach (String str in te.PayloadNames)
                {
                    sb.Append(str);
                    sb.Append(": ");
                    sb.Append(te.PayloadByName(str));
                    sb.Append(", ");
                }

                sb.Length -= 2;

                sb.Append(" }");
                writer.WriteLine(sb.ToString());
            }
            finally
            {
                sb.Clear();
                SbPool.Return(sb);
            }
        }

        internal virtual void Display(TraceEvent te, TextWriter output) => TraceDisplayer.DefaultDisplay(te, output);
    }
}