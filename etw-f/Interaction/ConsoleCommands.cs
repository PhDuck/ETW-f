namespace etw_f.Interaction
{
    using System;
    using System.IO;

    using Microsoft.Diagnostics.Tracing;

    using Capture;
    using Display;
    using Filtering;

    internal static class ConsoleCommands
    {
        internal static void AddConsoleKeyBindings(TraceCaptureSession session, TextWriter output, TextReader input)
        {
            Console.CancelKeyPress += (sender, arg) =>
            {
                session.EnterGate();

                try
                {
                    // After pressing CTRL + C we enter into a update mode.
                    output.WriteLine("Entered configuration mode, options:");
                    output.WriteLine("p: add/remove (p)roviders");
                    output.WriteLine("f: add/remove (f)ilters");
                    output.WriteLine("c: (c)ancel monitoring");
                    output.WriteLine("l: update (l)evel");
                    output.WriteLine("r: (r)eturn to monitoring");

                    // TODO: Now that everything uses input/output specified as args, this is a bit nasty.
                    Console.SetIn(input);
                    ConsoleKeyInfo key = Console.ReadKey();

                    switch (key.KeyChar)
                    {
                        case 'l':
                            HandleLevel(session, output, input);
                            arg.Cancel = true;
                            break;
                        case 'p':
                            HandleProvider(session, output, input);
                            arg.Cancel = true;
                            return;
                        case 'f':
                            HandleFilter(session, output, input);
                            arg.Cancel = true;
                            break;
                        case 'c':
                            output.WriteLine("Cancelling monitoring");
                            session.Dispose();
                            break;
                        case 's':
                            // TODO: SELECT/VIEW : Allows to specific which columns to show.
                            break;
                        case 'r':
                            arg.Cancel = true;
                            break;
                        default:
                            output.WriteLine($"Un-recognized key pressed {key.KeyChar}, stopping monitoring.");
                            session.Dispose();
                            break;
                    }
                }
                finally
                {
                    session.ReleaseGate();
                }
            };
        }

        private static void HandleLevel(TraceCaptureSession session, TextWriter writer, TextReader reader)
        {
            EventProvider provider = GetProvider(session, writer, reader) ??
                                     throw new ArgumentNullException(nameof(GetProvider));

            TraceEventLevel level = ParseLevel(writer, reader);

            if (provider.TraceEventLevel == level)
            {
                return;
            }

            // It seems one cannot alter it, so enable and re-enable.
            session.DisableProvider(provider);
            provider = EventProvider.Create(provider.Guid, level, provider._displayer);
            session.EnableProvider(provider);
        }

        private static readonly String[] _eventLevelsStr = Enum.GetNames(typeof(TraceEventLevel));
        
        private static TraceEventLevel ParseLevel(TextWriter writer, TextReader reader)
        {
            writer.Write("Select trace level: ");
            Int32 i = 0;
            foreach (String str in _eventLevelsStr)
            {
                writer.Write(i++);
                writer.Write(": ");
                writer.Write(str);
                writer.Write(", ");
            }
            writer.Write(Environment.NewLine);

            String option = reader.ReadLine() ?? throw new ArgumentNullException(nameof(option));

            if (!Byte.TryParse(option, out Byte selection))
            {
                throw new ArgumentException($"Selection outside of possible {option}");
            }

            if (selection >= _eventLevelsStr.Length)
            {
                throw new ArgumentException($"Selection outside range {selection}");
            }

            return (TraceEventLevel) selection;
        }

        private static void HandleProvider(TraceCaptureSession session, TextWriter output, TextReader input)
        {
            output.WriteLine("Specify provider name or guid:");
            String? providerId = input.ReadLine();

            // TODO: Handle trace level

            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new ArgumentException("Provider Id cannot be zero or null");
            }

            (Boolean result, Guid? providerGuid) = EventProvider.TryResolveProviderFromString(providerId);
            
            if (!result || !providerGuid.HasValue)
            {
                throw new ArgumentException($"Provider id: {providerId} not valid.");
            }

            (Boolean innerResult, EventProvider? eventProvider) = session.TryGetEventProvider(providerGuid.Value);
            if (innerResult && eventProvider != null)
            {
                session.DisableProvider(eventProvider);
                eventProvider.Dispose();
            }
            else
            {
                eventProvider = EventProvider.Create(providerId, TraceEventLevel.Always, TraceDisplayer.Default);
                session.EnableProvider(eventProvider);
            }
        }

        private static void HandleFilter(TraceCaptureSession session, TextWriter output, TextReader input)
        {
            EventProvider eventProvider = GetProvider(session, output, input);

            String eventName; String fieldName; String filter;
            try
            {
                output.WriteLine("Specify EventName");
                eventName = input.ReadLine() ?? throw new ArgumentNullException(nameof(eventName));
                output.WriteLine("Specify payload field name");
                fieldName = input.ReadLine() ?? throw new ArgumentNullException(nameof(fieldName));
                output.WriteLine("Specify filter");
                filter = input.ReadLine() ?? throw new ArgumentNullException(nameof(filter));
            }
            catch (ArgumentNullException)
            {
                session.Dispose();
                throw;
            }

            var fe = new PayloadFieldBoundFilterExpression(filter, fieldName);

            if (filter.Length == 0)
            {
                eventProvider.ClearFilter(eventName, fe);
            }
            else
            {
                eventProvider.AddOrUpdateFilter(eventName, fe);
            }
        }

        private static EventProvider GetProvider(TraceCaptureSession session, TextWriter output, TextReader input)
        {
            EventProvider eventProvider;
            switch (session.ProviderCount)
            {
                case 0:
                    throw new ArgumentException("No providers exists, please add provider first");
                case 1:
                    // Special case when only one provider exists:
                    eventProvider = session.GetSingleEventProvider();
                    break;
                default:
                {
                    output.WriteLine("Select provider:");
                    var providers = session.GetProviders();
                    for (Int32 i = 0; i < providers.Count; i++)
                    {
                        EventProvider ep = providers[i];
                        output.WriteLine($"{i}: {ep}");
                    }

                    String strSelection = input.ReadLine() ?? throw new ArgumentNullException(nameof(strSelection));
                    if (!UInt16.TryParse(strSelection, out UInt16 selection))
                    {
                        throw new ArgumentException($"Invalid input {strSelection}");
                    }

                    if (selection >= providers.Count)
                    {
                        throw new ArgumentException($"Invalid selection: {selection}");
                    }

                    return providers[selection];
                }
            }

            return eventProvider;
        }
    }
}