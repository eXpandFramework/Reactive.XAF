using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Xpand.Extensions.Tracing {
    /// <summary>
    /// Trace listener that writes to a text file, rolling to a new file based on a filename template (usually including the date).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A rolling log file is achieved by including the date in the filename, so that when the date changes
    /// a different file is used.
    /// </para>
    /// <para>
    /// Available tokens are DateTime (a UTC DateTimeOffset) and LocalDateTime (a local DateTimeOffset), 
    /// as well as ActivityId, AppData, AppDomain, ApplicationName, MachineName, 
    /// ProcessId, ProcessName, and User. These use standard .NET 
    /// format strings, e.g. "Trace{DateTime:yyyyMMddTHH}.log" would generate a different log name
    /// each hour.
    /// </para>
    /// <para>
    /// The default filePathTemplate is "{ApplicationName}-{DateTime:yyyy-MM-dd}.log", which matches
    /// the format used by Microsoft.VisualBasic.Logging.FileLogTraceListener (except that it uses
    /// UTC time instead of local time).
    /// </para>
    /// </remarks>
    public class RollingFileTraceListener : TraceListenerBase {
        // Default format matches Microsoft.VisualBasic.Logging.FileLogTraceListener
        private const string DefaultFilePathTemplate = "{ApplicationName}-{DateTime:yyyy-MM-dd}.log";

        // Default format matches Microsoft.VisualBasic.Logging.FileLogTraceListener
        public const string DefaultTemplate = "{DateTime:u} [{Thread}] {EventType} {Source} {Id}: {Message}{Data}";

        private static readonly string[] SupportedAttributes = {
            "template", "Template",
            "convertWriteToEvent", "ConvertWriteToEvent",
        };

        readonly TraceFormatter _traceFormatter = new();
        private readonly RollingTextWriter _rollingTextWriter;

        /// <summary>
        /// Constructor. Writes to a rolling text file using the default name.
        /// </summary>
        public RollingFileTraceListener()
            : this(DefaultFilePathTemplate) { }

        /// <summary>
        /// Constructor with initializeData.
        /// </summary>
        /// <param name="filePathTemplate">Template filename to log to; may use replacement parameters.</param>
        /// <remarks>
        /// <para>
        /// A rolling log file is achieved by including the date in the filename, so that when the date changes
        /// a different file is used.
        /// </para>
        /// <para>
        /// Available tokens are DateTime (a UTC DateTimeOffset) and LocalDateTime (a local DateTimeOffset), 
        /// as well as ApplicationName, ProcessId, ProcessName and MachineName. These use standard .NET 
        /// format strings, e.g. "Trace{DateTime:yyyyMMddTHH}.log" would generate a different log name
        /// each hour.
        /// </para>
        /// <para>
        /// The default filePathTemplate is "{ApplicationName}-{DateTime:yyyy-MM-dd}.log", which matches
        /// the format used by Microsoft.VisualBasic.Logging.FileLogTraceListener (except that it uses
        /// UTC time instead of local time).
        /// </para>
        /// <para>
        /// To get behaviour that exactly matches FileLogTraceListener, 
        /// use "{ApplicationName}-{LocalDateTime:yyyy-MM-dd}.log".
        /// </para>
        /// </remarks>
        public RollingFileTraceListener(string filePathTemplate) => _rollingTextWriter = string.IsNullOrEmpty(filePathTemplate)
            ? new RollingTextWriter(DefaultFilePathTemplate)
            : RollingTextWriter.Create(filePathTemplate);


        /// <summary>
        /// Gets or sets whether calls to the Trace class static Write and WriteLine methods should be converted to Verbose events,
        /// and then filtered and formatted (otherwise they are output directly to the file).
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults",
            MessageId = "System.Boolean.TryParse(System.String,System.Boolean@)",
            Justification = "Default value is acceptable if conversion fails.")]
        public bool ConvertWriteToEvent {
            get {
                // Default behaviour is to convert Write to event.
                var convertWriteToEvent = true;
                if (Attributes.ContainsKey("convertWriteToEvent")) {
                    bool.TryParse(Attributes["convertWriteToEvent"], out convertWriteToEvent);
                }

                return convertWriteToEvent;
            }
            set => Attributes["convertWriteToEvent"] = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets or sets the file system to use; this defaults to an adapter for System.IO.File.
        /// </summary>
        public IFileSystem FileSystem {
            get => _rollingTextWriter.FileSystem;
            set => _rollingTextWriter.FileSystem = value;
        }

        /// <summary>
        /// Gets whether the listener internally handles thread safety
        /// (or if the System.Diagnostics framework needs to co-ordinate threading).
        /// </summary>
        public override bool IsThreadSafe => true;

        /// <summary>
        /// Gets or sets the token format string to use to display trace output.
        /// </summary>
        /// <remarks>
        /// <para>
        /// See TraceFormatter for details of the supported formats.
        /// </para>
        /// <para>
        /// The default value is "{DateTime:u} [{Thread}] {EventType} {Source} {Id}: {Message}{Data}".
        /// </para>
        /// <para>
        /// To get a format that matches Microsoft.VisualBasic.Logging.FileLogTraceListener, 
        /// use the tab delimited format "{Source}&#x9;{EventType}&#x9;{Id}&#x9;{Message}{Data}". 
        /// (Note: In the config XML file the TAB characters are XML encoded; if specifying 
        /// in C# code the delimiters  would be '\t'.)
        /// </para>
        /// <para>
        /// To get a format matching FileLogTraceListener with all TraceOutputOptions enabled, use
        /// "{Source}&#x9;{EventType}&#x9;{Id}&#x9;{Message}&#x9;{Callstack}&#x9;{LogicalOperationStack}&#x9;{DateTime:u}&#x9;{ProcessId}&#x9;{ThreadId}&#x9;{Timestamp}&#x9;{MachineName}".
        /// </para>
        /// <para>
        /// To get a format similar to that produced by System.Diagnostics.TextWriterTraceListener,
        /// use "{Source} {EventType}: {Id} : {Message}{Data}".
        /// </para>
        /// </remarks>
        public string Template {
            get => Attributes.ContainsKey("template") ? Attributes["template"] : DefaultTemplate;
            set => Attributes["template"] = value;
        }

        /// <summary>
        /// Gets the template for the rolling file name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is part of initializeData; if the value changes the
        /// listener is recreated. See the constructor parameter for details
        /// of the supported formats.
        /// </para>
        /// </remarks>
        public string FilePathTemplate => _rollingTextWriter.FilePathTemplate;

        /// <summary>
        /// Flushes the output buffer.
        /// </summary>
        public override void Flush() {
            _rollingTextWriter.Flush();
        }

        /// <summary>
        /// Allowed attributes for this trace listener.
        /// </summary>
        protected override string[] GetSupportedAttributes() => SupportedAttributes;

        /// <summary>
        /// Handles trace Write calls - either output directly to console or convert to verbose event
        /// based on the ConvertWriteToEvent setting.
        /// </summary>
        protected override void Write(string category, string message, object data) {
            // Either format as event or write direct to stream based on flag.
            if (ConvertWriteToEvent) {
                base.Write(category, message, data);
            }
            else {
                _rollingTextWriter.Write(null, message);
            }
        }

        /// <summary>
        /// Handles trace Write calls - either output directly to console or convert to verbose event
        /// based on the ConvertWriteToEvent setting.
        /// </summary>
        protected override void WriteLine(string category, string message, object data) {
            // Either format as event or write direct to stream based on flag.
            if (ConvertWriteToEvent) {
                base.WriteLine(category, message, data);
            }
            else {
                _rollingTextWriter.WriteLine(null, message);
            }
        }

        /// <summary>
        /// Write trace event with data.
        /// </summary>
        protected override void WriteTrace(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message, Guid? relatedActivityId, object[] data) {
            // NOTE: In Microsoft.VisualBasic.Logging.FileLogTraceListener the following are output separated 
            // by a configurable delimiter: 
            //   source, TraceEventType, id, message, 
            // Then, based on TraceOutputOptions:
            //   call stack, logical operation stack, datetime (UTC, format "u"), process id, thread id, timestamp
            // Then, based on listener options:
            //   host name

            // Supporting a template string kind of makes TraceOutputOptions redundant,
            // but could support for backwards compatibility. 
            // i.e. if any T-O-O are set, then append them anyway??

            var output = _traceFormatter.Format(Template,
                this,
                eventCache,
                source,
                eventType,
                id,
                message,
                relatedActivityId,
                data
            );
            _rollingTextWriter.WriteLine(eventCache, output);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (_rollingTextWriter != null) {
                    _rollingTextWriter.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}