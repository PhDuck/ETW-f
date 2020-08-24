namespace etw_f.Capture
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Diagnostics.Tracing.Session;

    using Display;
    using Misc;

    internal class TraceCaptureSession : IDisposable
    {
        private readonly TextWriter _textWriter;
        private readonly TextReader _textReader;
        private readonly TraceEventSession _session;
        private readonly Dictionary<Guid, EventProvider> _traceProviders = new Dictionary<Guid, EventProvider>();

        /// <summary>
        /// Events that are queued for later consumption.
        /// </summary>
        private readonly Queue<TraceEvent> _eventQueue = new Queue<TraceEvent>();

        /// <summary>
        /// The limit as to how many events are queued, if more they are dropped.
        /// </summary>
        private const Int32 MAX_QUEUE_SIZE = 16384;

        /// <summary>
        /// A simple gate the can be locked or open.
        /// </summary>
        private readonly Gate _gate;

        private TraceCaptureSession(String sessionName, TextWriter writer, TextReader reader)
        {
            this._textWriter = writer;
            this._textReader = reader;
            this._session = new TraceEventSession(sessionName);
            this._gate = new Gate();
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
            this._session.Source.Dynamic.All += this.HandleDynamicEvent;
        }

        private void HandleDynamicEvent(TraceEvent? e)
        {
            EventProvider? ep = null;

            // The gate might be taken while we are handling events. This is okay since it would essentially finish immediately.
            // For cases where it is running through queue, it would mean you paused the handling,
            // then began handling and immediately enter config mode again.
            // If this becomes a real problem, we can always check the gate at each iteration.
            // But that might be a problem if no other events if event callbacks are synchronous.
            if (e != null && this._gate.TestGate())
            {
                do
                {
                    if ((ep != null && !ep.Disposed && ep.Guid.Equals(e.ProviderGuid)) || // Don't load out from dict if event is for same provider.
                        this._traceProviders.TryGetValue(e.ProviderGuid, out ep))
                    {
                        if (ep == null)
                        {
                            throw new ArgumentNullException(nameof(ep));
                        }

                        if (ep.TraceEventLevel > e.Level)
                        {
                            // Queue events might have an old trace level, verify again.
                            continue;
                        }

                        ep.HandleEvent(e, this._textWriter);
                    }
                } while (this._eventQueue.TryDequeue(out e));
            }
            else
            {
                if (this._eventQueue.Count >= MAX_QUEUE_SIZE || e == null)
                {
                    return;
                }

                // Clone is necessary since the same event is re-used, else we would just keep points to same object.
                TraceEvent te = e.Clone();

                this._eventQueue.Enqueue(te);
            }
        }

        internal void EnterGate() => this._gate.EnterGate();

        internal void ReleaseGate() => this._gate.ReleaseGate();

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