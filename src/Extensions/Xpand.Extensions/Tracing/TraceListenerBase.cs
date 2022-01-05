using System;
using System.Diagnostics;
using System.Globalization;

namespace Xpand.Extensions.Tracing {
    /// <summary>
    /// Trace listener that forwards all calls to a single template method,
    /// allowing easy implementation of custom trace listeners.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This trace listener is designed to allow easy creation of custom
    /// trace listeners by inheriting from this class and implementing
    /// a single template method, WriteTrace.
    /// </para>
    /// <para>
    /// The WriteTrace method combines all the various combinations
    /// of trace methods from the base TraceListener class, passing
    /// the full details to the template method allowing structured
    /// logging.
    /// </para>
    /// <para>
    /// By default the write methods are also converted to Verbose trace
    /// events, allowing them to also be logged in a structured manner.
    /// </para>
    /// <para>
    /// If implementing a stream-based listener, then the default
    /// template methods for Write and WriteLine can also be overridden
    /// to provide behaviour similar to stream-based listeners in
    /// the .NET Framework.
    /// </para>
    /// </remarks>
    public abstract class TraceListenerBase : TraceListener {
        const string CategorySeparator = ": ";

        // //////////////////////////////////////////////////////////
        // Constructors

        /// <summary>
        /// Constructor used when creating from config file. 
        /// (The Name property is set after the TraceListener is created.)
        /// </summary>
        protected TraceListenerBase() { }

        /// <summary>
        /// Constructor used when creating dynamically in code. The name should be set in the constructor.
        /// </summary>
        /// <param name="name">Name of the trace listener.</param>
        protected TraceListenerBase(string name)
            : base(name) { }


        // //////////////////////////////////////////////////////////
        // Public Methods

        /// <summary>
        /// Closes the listener so it no longer receives tracing or debugging output.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation calls Flush().
        /// </para>
        /// </remarks>
        public override void Close() {
            // Always Flush before Close
            Flush();
            base.Close();
        }

        /// <summary>
        /// Writes the trace data to the listener output, if allowed by the configured filter.
        /// The data is forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            object data) {
            if ((Filter == null) ||
                Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null)) {
                WriteTrace(eventCache, source, eventType, id, null, null, new[] { data });
            }
        }

        /// <summary>
        /// Writes the trace data to the listener output, if allowed by the configured filter.
        /// The data is forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            params object[] data) {
            if ((Filter == null) ||
                Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data)) {
                WriteTrace(eventCache, source, eventType, id, null, null, data);
            }
        }

        /// <summary>
        /// Writes the event to the listener, if allowed by the configured filter.
        /// The event is forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message) {
            if ((Filter == null) ||
                Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null)) {
                WriteTrace(eventCache, source, eventType, id, message, null, null);
            }
        }

        /// <summary>
        /// Writes the event to the listener, formatting the message with the provided arguments, 
        /// but only if allowed by the configured filter.
        /// The event is forwarded to the WriteTraceFormat template method if it has args, or WriteTrace without.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For performance reasons the message is not formatted, 
        /// and the args not converted to strings, 
        /// unless the filter is first passed.
        /// </para>
        /// </remarks>
        public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args) {
            if ((Filter == null) ||
                Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null)) {
                // Note: traceSource.TraceInformation(message) calls TraceEvent(..., format, null) 
                // not TraceEvent(..., message), so we don't call string.Format if args is null.
                // This means that calling traceSource.TraceEvent(..., format, [object[]]null) works,
                // instead of throwing ArgumentNullException (as it should).
                // Design decision to have TraceInformation(message) work than to be 100% correct in 
                // throwing exceptions when args is null.
                {
                    WriteTraceFormat(eventCache, source, eventType, id, format, args);
                }
            }
        }

        /// <summary>
        /// Writes the transfer to the listener, if allowed by the configured filter.
        /// The transfer is forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message,
            Guid relatedActivityId) {
            if ((Filter == null) ||
                Filter.ShouldTrace(eventCache, source, TraceEventType.Transfer, id, message, null, null, null)) {
                var traceMessage = string.Format(CultureInfo.CurrentCulture, "{0}, relatedActivityId={1}", message, relatedActivityId.ToString());
                WriteTrace(eventCache, source, TraceEventType.Transfer, id, traceMessage, relatedActivityId, null);
            }
        }

        /// <summary>
        /// Writes the object to the listener.
        /// The object is forward to the Write template method as data,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void Write(object o) {
            Write(null, null, o);
        }

        /// <summary>
        /// Writes the object to the listener with the specified category.
        /// The object is forwarded to the Write template method as data,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void Write(object o, string category) {
            Write(category, null, o);
        }

        /// <summary>
        /// Writes the message to the listener.
        /// The message is forwarded to the Write template method,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void Write(string message) {
            Write(null, message, null);
        }

        /// <summary>
        /// Writes the message to the listener with the specified category.
        /// The message is forwarded to the Write template method,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void Write(string message, string category) {
            Write(category, message, null);
        }

        /// <summary>
        /// Writes the object to the listener.
        /// The object is forwarded to the Write template method as data,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void WriteLine(object o) {
            WriteLine(null, null, o);
        }

        /// <summary>
        /// Writes the object to the listener with the specified category.
        /// The object is forwarded to the Write template method as data,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void WriteLine(object o, string category) {
            WriteLine(category, null, o);
        }

        /// <summary>
        /// Writes the message to the listener.
        /// The message is forwarded to the Write template method,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void WriteLine(string message) {
            WriteLine(null, message, null);
        }

        /// <summary>
        /// Writes the message to the listener with the specified category.
        /// The message is forwarded to the Write template method,
        /// which by default is then forwarded to the WriteTrace template method.
        /// </summary>
        public sealed override void WriteLine(string message, string category) {
            WriteLine(category, message, null);
        }


        // //////////////////////////////////////////////////////////
        // Protected

        /// <summary>
        /// Template method that by default forwards the written details 
        /// to WriteTrace as a Verbose event
        /// this should be overridden to write direct to the output if using a stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation converts the written details to a Verbose event
        /// which is forwarded to WriteTrace for output.
        /// </para>
        /// <para>
        /// The category is prepended to any message with a separating colon (:),
        /// which is similar to the format used by framework listeners.
        /// The data object is traced as data, which usually means it is
        /// also eventually output as text, but usually in a different field. 
        /// If category is specified when tracing a data object, the category
        /// is treated as the message (without any colon).
        /// </para>
        /// <para>
        /// If the listener is using a stream based output, then usually the
        /// Write and WriteLine methods should be overridden to output directly to
        /// the stream (with a following newline in the case of WriteLine).
        /// </para>
        /// <para>
        /// Listeners should also consider supporting a ConvertWriteToEvent property,
        /// which if set calls this base implementation, allowing the user to choose
        /// if Write and WriteLine should be output directly or treated as events
        /// (and formatted accordingly).
        /// </para>
        /// <para>
        /// Listeners that do not use a continuous stream based output, but use
        /// specifically formatted output, should leave this default implementation
        /// and simply override WriteTrace.
        /// </para>
        /// </remarks>
        protected virtual void Write(string category, string message, object data) {
            TraceWriteAsEvent(category, message, data);
        }

        /// <summary>
        /// Template method that by default forwards the written details 
        /// to WriteTrace as a Verbose event
        /// this should be overridden to write direct to the output as a complete line 
        /// (with a newline at the end) if using a stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation converts the written details to a Verbose event
        /// which is forwarded to WriteTrace for output.
        /// </para>
        /// <para>
        /// The category is prepended to any message with a separating colon (:),
        /// which is similar to the format used by framework listeners.
        /// The data object is traced as data, which usually means it is
        /// also eventually output as text, but usually in a different field. 
        /// If category is specified when tracing a data object, the category
        /// is treated as the message (without any colon).
        /// </para>
        /// <para>
        /// If the listener is using a stream based output, then usually the
        /// Write and WriteLine methods should be overridden to output directly to
        /// the stream (with a following newline in the case of WriteLine).
        /// </para>
        /// <para>
        /// Listeners should also consider supporting a ConvertWriteToEvent property,
        /// which if set calls this base implementation, allowing the user to choose
        /// if Write and WriteLine should be output directly or treated as events
        /// (and formatted accordingly).
        /// </para>
        /// <para>
        /// Listeners that do not use a continuous stream based output, but use
        /// specifically formatted output, should leave this default implementation
        /// and simply override WriteTrace.
        /// </para>
        /// </remarks>
        protected virtual void WriteLine(string category, string message, object data) {
            TraceWriteAsEvent(category, message, data);
        }

        /// <summary>
        /// Virtual method that can be overridden by listeners who want to handle the format strings
        /// before the args are resolved.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation of this message uses string.Format() to resolve the args, and
        /// then passes the result to WriteTrace.
        /// </para>
        /// <para>
        /// If listeners want to intercept the message, after it has been filtered, but before the args
        /// have been resolved, then they can override this method and provide an alternative implementation.
        /// For example, Seq records the original format separate from the args, so that it can filter on
        /// the message 'type' (based on a hash of the fixed format, ignoring the variable args) and separately
        /// also filter on the separate arg values.
        /// </para>
        /// </remarks>
        protected virtual void WriteTraceFormat(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args) {
            var message = string.Format(CultureInfo.CurrentCulture, format, args);
            WriteTrace(eventCache, source, eventType, id, message, null, null);
        }

        /// <summary>
        /// Template method that must be overridden to write trace to the listener output.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All of the parent interface of TraceListener is forwarded to this single
        /// template method including, by default, calls to Write and WriteLine methods.
        /// </para>
        /// <para>
        /// This means when inheriting from TraceListenerBase only this single
        /// method needs to be overridden to implement a complete listener.
        /// </para>
        /// <para>
        /// Listeners that use a continuous stream based output may also consider
        /// overriding the two Write and WriteLine template methods to provide
        /// direct writing behaviour, although this is not required.
        /// </para>
        /// </remarks>
        protected abstract void WriteTrace(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message, Guid? relatedActivityId, object[] data);


        // //////////////////////////////////////////////////////////
        // Private

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters",
            MessageId =
                "Essential.Diagnostics.TraceListenerBase.WriteTrace(System.Diagnostics.TraceEventCache,System.String,System.Diagnostics.TraceEventType,System.Int32,System.String,System.Nullable<System.Guid>,System.Object[])")]
        private void TraceWriteAsEvent(string category, string message, object data) {
            if ((Filter == null) || Filter.ShouldTrace(null, null!, TraceEventType.Verbose, 0, message,
                    new object[] { category }, data, null)) {
                if (data == null) {
                    if (category != null) {
                        var categoryMessage = category + CategorySeparator + message;
                        WriteTrace(null, null, TraceEventType.Verbose, 0, categoryMessage, null, null);
                    }
                    else {
                        WriteTrace(null, null, TraceEventType.Verbose, 0, message, null, null);
                    }
                }
                else {
                    WriteTrace(null, null, TraceEventType.Verbose, 0, category, null, new[] { data });
                }
            }
        }
    }
}