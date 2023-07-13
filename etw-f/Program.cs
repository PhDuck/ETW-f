using System;

namespace etw_f
{
    using Capture;
    using Display;

    using etw_f.Filtering;
    using etw_f.Input;

    using Microsoft.Diagnostics.Tracing;

    using System.Collections.Generic;

    static class Program
    {
        private const String DefaultSessionName = "ETW-f";

        static void Main(String[] args)
        {
            InputArguments inputArguments = InputHandler.Parse(args);

            using (var session = TraceCaptureSession.Create(DefaultSessionName, Console.Out, Console.In))
            {
                foreach (string provider in inputArguments.Providers)
                {
                    EventProvider ep = EventProvider.Create(provider, TraceEventLevel.Always, TraceDisplayer.Default);
                    session.EnableProvider(ep);
                }

                foreach ((string eventname, FilterExpression fe) in inputArguments.Filters)
                {
                    if (session.ProviderCount > 1)
                    {
                        throw new InvalidOperationException("Not supported atm.");
                    }

                    EventProvider ep = session.GetSingleEventProvider();

                    ep.SetFilter(eventname, fe);
                }

                foreach ((string eventName, IReadOnlyList<string> selectedFields) in inputArguments.SelectedFields)
                {
                    if (session.ProviderCount > 1)
                    {
                        throw new InvalidOperationException("Not supported atm.");
                    }

                    EventProvider ep = session.GetSingleEventProvider();

                    ep.AddOrUpdateView(eventName, selectedFields);
                }

                session.AddCallback();

                session.Process();
            }
        }
    }
}
