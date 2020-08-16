using System;

namespace etw_f
{
    using Microsoft.Diagnostics.Tracing;

    using Capture;
    using Display;

    static class Program
    {
        private const String DefaultSessionName = "ETW-f";
        
        static void Main(String[] args)
        {
            using (var session = TraceCaptureSession.Create(DefaultSessionName, Console.Out, Console.In))
            {
                foreach (var providerId in args[0].Split(','))
                {
                    EventProvider ep = EventProvider.Create(providerId, TraceEventLevel.Always, TraceDisplayer.Default);
                    session.EnableProvider(ep);
                }

                session.AddCallback();

                session.Process();
            }
        }
    }
}