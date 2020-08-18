namespace etw_f.Capture
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Diagnostics.Tracing.Session;

    using Display;
    using Filtering;

    internal class EventProvider : IDisposable, IEquatable<EventProvider>
    {
        public readonly Guid Guid;
        public readonly String Name;
        internal readonly TraceDisplayer _displayer;
        internal readonly TraceEventLevel TraceEventLevel;

        private readonly Dictionary<String, List<FilterExpression>> _filters;

        private EventProvider(Guid guid, TraceEventLevel traceEventLevel, TraceDisplayer displayer)
        {
            this.Guid = guid;
            this._displayer = displayer;
            this._filters = new Dictionary<String, List<FilterExpression>>();
            this.TraceEventLevel = traceEventLevel;
            this.Name = TraceEventProviders.GetProviderName(guid);
        }

        public static EventProvider Create(Guid g, TraceEventLevel traceEventLevel, TraceDisplayer traceDisplayer)
        {
            if (traceDisplayer == null)
            {
                throw new ArgumentNullException(nameof(traceDisplayer));
            }

            return new EventProvider(g, traceEventLevel, traceDisplayer);
        }

        public static EventProvider Create(String providerName, TraceEventLevel traceEventLevel, TraceDisplayer traceDisplayer)
        {
            if (traceDisplayer == null)
            {
                throw new ArgumentNullException(nameof(traceDisplayer));
            }

            return new EventProvider(ResolveProviderFromString(providerName), traceEventLevel, traceDisplayer);
        }

        public static (Boolean result, Guid? providerGuid) TryResolveProviderFromString(String providerId)
        {
            if (Guid.TryParse(providerId, out Guid guid))
            {
                return (true, guid);
            }

            // Handle a provider based on name. E.g. Company-Service-Component, so lookup it's GUID.
            Guid providerGuid = TraceEventProviders.GetProviderGuidByName(providerId);
            if (providerGuid == Guid.Empty)
            {
                providerGuid = TraceEventProviders.GetEventSourceGuidFromName(providerId);
            }

            if (providerGuid == Guid.Empty)
            {
                return (false, null);
            }

            return (true, providerGuid);
        }

        public static Guid ResolveProviderFromString(String providerId)
        {
            var res = TryResolveProviderFromString(providerId);
            if (!res.result || res.providerGuid == null)
            {
                throw new ArgumentException($"Could not resolve guid from provider id: {providerId}");
            }

            return res.providerGuid.Value;
        }

        private static void ClearFilter(List<FilterExpression> filters, FilterExpression fe)
        {
            // First check if an existing comparable filter exists.
            // filter: fieldName: age, pattern: 0 != fieldName: age, pattern: 32

            if (fe is PayloadFieldBoundFilterExpression fieldBound)
            {
                var f = filters.First(f => f is PayloadFieldBoundFilterExpression fpfbfe
                                                 && fieldBound.FieldName.Equals(fpfbfe.FieldName));
                if (f == null)
                {
                    // No filter found
                    return;
                }

                filters.Remove(f);
            }
            else
            {
                // TODO: Deal with all other filter types when they become relevant.
            }
        }

        internal void AddOrUpdateFilter(String eventName, FilterExpression fe)
        {
            if (this._filters.TryGetValue(eventName, out List<FilterExpression>? fes))
            {
                if (fes == null)
                {
                    throw new ArgumentNullException(nameof(fes));
                }

                ClearFilter(fes, fe);
                fes.Add(fe);
            }
            else
            {
                var filters = new List<FilterExpression>() { fe };
                this._filters.Add(eventName, filters);
            }
        }

        internal void ClearFilter(String eventName, FilterExpression fe)
        {
            if (!this._filters.TryGetValue(eventName, out List<FilterExpression>? filters))
            {
                return;
            }

            if (filters == null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            ClearFilter(filters, fe);
        }

        public void HandleEvent(TraceEvent te, TextWriter output)
        {
            if (this.ApplyFiltering(te))
            {
                this._displayer.Display(te, output);
            }
        }

        private Boolean ApplyFiltering(TraceEvent te)
        {
            if (this._filters.Count != 0 && this._filters.TryGetValue(te.EventName, out var filters))
            {
                if (!filters.Any(f => f.Evaluate(te)))
                {
                    return false;
                }
            }

            return true;
        }

        public override Boolean Equals(Object? obj)
        {
            return this.Equals(obj as EventProvider);
        }

        public Boolean Equals(EventProvider? ep)
        {
            return ep != null && this.Guid == ep.Guid;
        }

        public override Int32 GetHashCode()
        {
            return this.Guid.GetHashCode();
        }

        public override String ToString()
        {
            String guidString = this.Guid.ToString();
            if (this.Name == guidString)
            {
                return this.Name;
            }

            return String.Concat(this.Name, " ", guidString);
        }

        internal Boolean Disposed = false;

        public void Dispose()
        {
            this.Disposed = true;
        }
    }
}