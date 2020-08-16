namespace etw_f.Capture
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Diagnostics.Tracing.Session;

    using Display;

    internal class TraceCaptureSession : IDisposable
    {
        private readonly TextWriter _textWriter;
        private readonly TextReader _textReader;
        private readonly TraceEventSession _session;
        private readonly Dictionary<Guid, EventProvider> _traceProviders = new Dictionary<Guid, EventProvider>();

        private TraceCaptureSession(String sessionName, TextWriter writer, TextReader reader)
        {
            this._textWriter = writer;
            this._textReader = reader;
            this._session = new TraceEventSession(sessionName);
        }

        public static TraceCaptureSession Create(String sessionName, TextWriter writer, TextReader reader)
        {
            Boolean elevationStatus = TraceEventSession.IsElevated() ?? false;
            if (!elevationStatus)
            {
                throw new InvalidOperationException("You must have admin rights");
            }

            var traceCaptureSession = new TraceCaptureSession(sessionName, writer, reader);

            // Setup console settings
            Interaction.ConsoleCommands.AddConsoleKeyBindings(traceCaptureSession, traceCaptureSession._textWriter, traceCaptureSession._textReader);

            return traceCaptureSession;
        }

        #region Providers

        internal Boolean EnableProvider(EventProvider ep)
        {
            if (ep == null)
            {
                throw new ArgumentNullException(nameof(ep));
            }

            this._session.EnableProvider(ep.Guid, ep.TraceEventLevel);
            this._traceProviders.Add(ep.Guid, ep);

            return true;
        }

        internal Boolean DisableProvider(EventProvider provider)
        {
            // No guarantee that the provider is registered in this session, so ensure it.
            if (this._traceProviders.TryGetValue(provider.Guid, out EventProvider? ep) && ep != null)
            {
                this._session.DisableProvider(ep.Guid);
                this._traceProviders.Remove(ep.Guid);
            }
            else
            {
                throw new ArgumentException($"Event provider: {provider.Guid} is not enabled");
            }

            return true;
        }

        internal Int32 ProviderCount => this._traceProviders.Count;

        internal EventProvider GetSingleEventProvider()
        {
            if (this.ProviderCount != 1)
            {
                throw new ArgumentException("More than one provider exists.");
            }

            return this._traceProviders.First().Value;
        }

        internal EventProvider GetEventProvider(String providerId)
        {
            Guid providerGuid = EventProvider.ResolveProviderFromString(providerId);

            if (this._traceProviders.TryGetValue(providerGuid, out EventProvider? ep) && ep != null)
            {
                return ep;
            }

            throw new ArgumentException("Could not resolve event provider.");
        }

        internal (Boolean result, EventProvider? eventProvider) TryGetEventProvider(Guid providerGuid)
        {
            if (this._traceProviders.TryGetValue(providerGuid, out EventProvider? eventProvider) &&
                eventProvider != null)
            {
                return (true, eventProvider);
            }

            return (false, null);
        }

        internal IReadOnlyList<EventProvider> GetProviders()
        {
            return this._traceProviders.Select(kvp => kvp.Value).ToList();
        }

        #endregion

        internal void AddCallback()
        {
            TraceEventSchema.WriteHeaders(this._textWriter);
            this._session.Source.Dynamic.All += (e) =>
            {
                if (this._traceProviders.TryGetValue(e.ProviderGuid, out EventProvider? ep))
                {
                    if (ep == null)
                    {
                        throw new ArgumentNullException(nameof(ep));
                    }

                    ep.HandleEvent(e, this._textWriter);
                }
                else
                {
                    throw new ArgumentException($"No provider registered for {e.ProviderGuid}");
                }
            };
        }

        internal void Process()
        {
            this._session.Source.Process();
        }

        public void Dispose()
        {
            this._session.Dispose();
        }
    }
}