using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;

namespace Xpand.Extensions.Tracing {
    static class NativeMethods {
        public static HandleRef NullHandleRef = new(null, IntPtr.Zero);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
            "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule",
            Justification =
                "Rule should only apply to .NET 4.0. See http://connect.microsoft.com/VisualStudio/feedback/details/729254/bogus-ca5122-warning-about-p-invoke-declarations-should-not-be-safe-critical")]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetModuleFileName(HandleRef hModule, StringBuilder buffer, int length);
    }

    /// <summary>
    /// Adapter that wraps HttpContext.Current.
    /// </summary>
    public class HttpContextCurrentAdapter : IHttpTraceContext {
        static string AppData = "~/App_Data";

        /// <summary>
        /// Gets the physical file path that corresponds to the App_Data directory, if in the context of a web request.
        /// </summary>
        public string AppDataPath {
            get {
                var context = AppDomain.CurrentDomain.Web().HttpContext();
                if (context == null) {
                    return null;
                }

                string path = null;
                var server = context.GetPropertyValue("Server");
                if (server != null) {
                    //AppDomain.CurrentDomain.GetData("DataDirectory");
                    //return context.Server.MapPath(AppData);
                    //HttpRuntime.AppDomainAppVirtualPath
                    path = (string)server.CallMethod("MapPath", AppData);
                }

                return path;
            }
        }

        /// <summary>
        /// Gets the virtual path of the current request, if in the context of a web request. 
        /// </summary>
        public string RequestPath {
            get {
                var context = AppDomain.CurrentDomain.Web().HttpContext();
                if (context == null) return null;
                try {
                    var request = AppDomain.CurrentDomain.Web().GetPropertyValue("Request");
                    if (request == null) return null;
                    return (string)request.GetPropertyValue("Path");
                }
                catch (Exception) {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets information about the URL of the current request, if in the context of a web request. 
        /// </summary>
        public Uri RequestUrl {
            get {
                var context = AppDomain.CurrentDomain.Web().HttpContext();
                if (context == null) return null;
                try {
                    var request = context.GetPropertyValue("Request");
                    if (request == null) return null;
                    return (Uri)context.GetPropertyValue("Url");
                }
                catch (Exception) {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the IP host address of the remote client, if in the context of a web request. 
        /// </summary>
        public string UserHostAddress {
            get {
                var context = AppDomain.CurrentDomain.Web().HttpContext();
                if (context == null) return null;
                try {
                    var request = context.GetPropertyValue("Request");
                    if (request == null) return null;
                    return (string)request.GetPropertyValue("UserHostAddress");
                }
                catch (Exception) {
                    return null;
                }
            }
        }
    }

    public interface IHttpTraceContext {
        /// <summary>
        /// Gets the physical file path that corresponds to the App_Data directory, if in the context of a web request.
        /// </summary>
        string AppDataPath { get; }

        /// <summary>
        /// Gets the virtual path of the current request, if in the context of a web request. 
        /// </summary>
        string RequestPath { get; }

        /// <summary>
        /// Gets information about the URL of the current request, if in the context of a web request. 
        /// </summary>
        Uri RequestUrl { get; }

        /// <summary>
        /// Gets the IP host address of the remote client, if in the context of a web request. 
        /// </summary>
        string UserHostAddress { get; }
    }

    /// <summary>
    /// Formats trace output using a template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses the StringTemplate.Format function to format trace output using a supplied template
    /// and trace information. The formatted event can then be written to the console, a
    /// file, or other text-based output.
    /// </para>
    /// <para>
    /// The following parameters are available in the template string:
    /// Data, Data0, EventType, Id, Message, ActivityId, RelatedActivityId, Source, 
    /// Callstack, DateTime (or UtcDateTime), LocalDateTime, LogicalOperationStack, 
    /// ProcessId, ThreadId, Timestamp, MachineName, ProcessName, ThreadName,
    /// ApplicationName, MessagePrefix, AppDomain.
    /// </para>
    /// <para>
    /// An example template that generates the same output as the ConsoleListener is:
    /// "{Source} {EventType}: {Id} : {Message}".
    /// </para>
    /// </remarks>
    public class TraceFormatter {
        const int MaxPrefixLength = 40;
        const string PrefixContinuation = "...";

        static readonly Regex ControlCharRegex = new(@"\p{C}", RegexOptions.Compiled);

        string _applicationName;
        int _processId;
        string _processName;

        /// <summary>
        /// Gets or sets the context for ASP.NET web trace information.
        /// </summary>
        public IHttpTraceContext HttpTraceContext { get; set; } = new HttpContextCurrentAdapter();

        /// <summary>
        /// Formats a trace event, inserted the provided values into the provided template.
        /// </summary>
        /// <returns>A string containing the values formatted using the provided template.</returns>
        /// <remarks>
        /// <para>
        /// Obsolete. Should use the overload that includes listener instead.
        /// </para>
        /// </remarks>
        [Obsolete("Should use the overload that includes listener instead.")]
        public string Format(string template, TraceEventCache eventCache, string source,
            TraceEventType eventType, int id, string message,
            Guid? relatedActivityId, object[] data) {
            return Format(template, null, eventCache, source, eventType, id, message, relatedActivityId, data);
        }

        /// <summary>
        /// Formats a trace event, inserted the provided values into the provided template.
        /// </summary>
        /// <returns>A string containing the values formatted using the provided template.</returns>
        /// <remarks>
        /// <para>
        /// Uses the StringTemplate.Format function to format trace output using a supplied template
        /// and trace information. The formatted event can then be written to the console, a
        /// file, or other text-based output.
        /// </para>
        /// <para>
        /// The following parameters are available in the template string:
        /// Data, Data0, EventType, Id, Message, ActivityId, RelatedActivityId, Source, 
        /// Callstack, DateTime (or UtcDateTime), LocalDateTime, LogicalOperationStack, 
        /// ProcessId, ThreadId, Timestamp, MachineName, ProcessName, ThreadName,
        /// ApplicationName, PrincipalName, WindowsIdentityName.
        /// </para>
        /// <para>
        /// An example template that generates the same output as the ConsoleListener is:
        /// "{Source} {EventType}: {Id} : {Message}".
        /// </para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework",
            MessageId = "System.DateTimeOffset", Justification = "Deliberate dependency, .NET 2.0 SP1 required.")]
        public string Format(string template, TraceListener listener, TraceEventCache eventCache,
            string source, TraceEventType eventType, int id, string message,
            Guid? relatedActivityId, object[] data) {
            var result = StringTemplate.Format(CultureInfo.CurrentCulture, template,
                delegate(string name, out object value) {
                    switch (name.ToUpperInvariant()) {
                        case "EVENTTYPE":
                            value = eventType;
                            break;
                        case "ID":
                            value = id;
                            break;
                        case "MESSAGE":
                            value = message;
                            break;
                        case "MESSAGEPREFIX":
                            value = FormatPrefix(message);
                            break;
                        case "SOURCE":
                            value = source;
                            break;
                        case "DATETIME":
                        case "UTCDATETIME":
                            value = FormatUniversalTime(eventCache);
                            break;
                        case "LOCALDATETIME":
                            value = FormatLocalTime(eventCache);
                            break;
                        case "THREADID":
                            value = FormatThreadId(eventCache);
                            break;
                        case "THREAD":
                            value = Thread.CurrentThread.Name ?? FormatThreadId(eventCache);
                            break;
                        case "THREADNAME":
                            value = Thread.CurrentThread.Name;
                            break;
                        case "ACTIVITYID":
                            value = Trace.CorrelationManager.ActivityId;
                            break;
                        case "RELATEDACTIVITYID":
                            value = relatedActivityId;
                            break;
                        case "DATA":
                            value = FormatData(data);
                            break;
                        case "DATA0":
                            value = FormatData(data, 0);
                            break;
                        case "CALLSTACK":
                            value = FormatCallstack(eventCache);
                            break;
                        case "LOGICALOPERATIONSTACK":
                            value = FormatLogicalOperationStack(eventCache);
                            break;
                        case "PROCESSID":
                            value = FormatProcessId(eventCache);
                            break;
                        case "TIMESTAMP":
                            value = FormatTimeStamp(eventCache);
                            break;
                        case "MACHINENAME":
                            value = Environment.MachineName;
                            break;
                        case "PROCESSNAME":
                            value = FormatProcessName();
                            break;
                        case "USER":
                            value = Environment.UserDomainName + "\\" + Environment.UserName;
                            break;
                        case "PROCESS":
                            value = Environment.CommandLine;
                            break;
                        case "APPLICATIONNAME":
                            value = FormatApplicationName();
                            break;
                        case "APPDOMAIN":
                            value = AppDomain.CurrentDomain.FriendlyName;
                            break;
                        case "PRINCIPALNAME":
                            value = FormatPrincipalName();
                            break;
                        case "WINDOWSIDENTITYNAME":
                            throw new NotImplementedException();
                        // value = FormatWindowsIdentityName();
                        case "REQUESTURL":
                            value = HttpTraceContext.RequestUrl;
                            break;
                        case "REQUESTPATH":
                            value = HttpTraceContext.RequestPath;
                            break;
                        case "USERHOSTADDRESS":
                            value = HttpTraceContext.UserHostAddress;
                            break;
                        case "APPDATA":
                            value = HttpTraceContext.AppDataPath;
                            break;
                        case "LISTENER":
                            value = (listener == null) ? "" : listener.Name;
                            break;
                        default:
                            value = "{" + name + "}";
                            return true;
                    }

                    return true;
                });
            return result;
        }

        internal object FormatApplicationName() {
            EnsureApplicationName();
            object value = _applicationName;
            return value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework",
            MessageId = "System.DateTimeOffset", Justification = "Deliberate dependency, .NET 2.0 SP1 required.")]
        internal static object FormatLocalTime(TraceEventCache eventCache) {
            object value = eventCache == null ? DateTimeOffset.Now : ((DateTimeOffset)eventCache.DateTime).ToLocalTime();
            return value;
        }

        internal object FormatProcessId(TraceEventCache eventCache) {
            object value;
            if (eventCache == null) {
                EnsureProcessInfo();
                value = _processId;
            }
            else {
                value = eventCache.ProcessId;
            }

            return value;
        }

        internal object FormatProcessName() {
            EnsureProcessInfo();
            object value = _processName;
            return value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework",
            MessageId = "System.DateTimeOffset", Justification = "Deliberate dependency, .NET 2.0 SP1 required.")]
        internal static DateTimeOffset FormatUniversalTime(TraceEventCache eventCache) {
            DateTimeOffset value;
            value = eventCache == null ? DateTimeOffset.UtcNow : ((DateTimeOffset)eventCache.DateTime).ToUniversalTime();
            return value;
        }

        private void EnsureApplicationName() {
            if (_applicationName == null) {
                //applicationName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null) {
                    var moduleFileName = new StringBuilder(260);
                    int size = NativeMethods.GetModuleFileName(NativeMethods.NullHandleRef, moduleFileName,
                        moduleFileName.Capacity);
                    if (size > 0) {
                        _applicationName = Path.GetFileNameWithoutExtension(moduleFileName.ToString());
                        return;
                    }
                    //I don't want to raise any error here since I have a graceful result at the end.
                }

                _applicationName = Path.GetFileNameWithoutExtension(entryAssembly?.Location);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
            "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void EnsureProcessInfo() {
            if (_processName == null) {
                using (Process process = Process.GetCurrentProcess()) {
                    _processId = process.Id;
                    _processName = process.ProcessName;
                }
            }
        }

        private static object FormatCallstack(TraceEventCache eventCache) {
            object value = eventCache?.Callstack;

            return value;
        }

        private static object FormatData(object[] data) {
            StringBuilder builder = new StringBuilder();
            if (data != null) {
                for (int i = 0; i < data.Length; i++) {
                    if (i != 0) {
                        builder.Append(",");
                    }

                    if (data[i] != null) {
                        builder.Append(data[i]);
                    }
                }
            }

            object value = builder.ToString();
            return value;
        }

        private static object FormatData(object[] data, int index) {
            object value;
            if (data != null && data.Length > index) {
                value = data[index];
            }
            else {
                value = null;
            }

            return value;
        }

        private static object FormatLogicalOperationStack(TraceEventCache eventCache) {
            object value;
            var stack = eventCache == null ? Trace.CorrelationManager.LogicalOperationStack : eventCache.LogicalOperationStack;

            if (stack is { Count: > 0 }) {
                StringBuilder stackBuilder = new StringBuilder();
                foreach (object o in stack) {
                    if (stackBuilder.Length > 0) stackBuilder.Append(", ");
                    stackBuilder.Append(o);
                }

                value = stackBuilder.ToString();
            }
            else {
                value = null;
            }

            return value;
        }

        private static string FormatPrefix(string message) {
            if (!string.IsNullOrEmpty(message)) {
                // Prefix is the part of the message before the first <;,.:>
                string[] split = message.Split(new[] { '.', '!', '?', ':', ';', ',', '\r', '\n' }, 2,
                    StringSplitOptions.None);
                string prefix;
                if (split[0].Length <= MaxPrefixLength) {
                    prefix = split[0];
                }
                else {
                    prefix = split[0].Substring(0, MaxPrefixLength - PrefixContinuation.Length) + PrefixContinuation;
                }

                if (ControlCharRegex.IsMatch(prefix)) {
                    prefix = ControlCharRegex.Replace(prefix, "");
                }

                return prefix;
            }

            return message;
        }

        internal static object FormatPrincipalName() {
            var principal = Thread.CurrentPrincipal;
            object value = "";
            if (principal is { Identity:{ } }) {
                value = principal.Identity.Name;
            }

            return value;
        }

        internal static object FormatWindowsIdentityName() {
            // var identity = WindowsIdentity.GetCurrent();
            // object value = identity.Name;

            // return value;
            throw new NotImplementedException();
        }

        internal static object FormatThreadId(TraceEventCache eventCache) {
            object value;
            if (eventCache == null) {
                value = Thread.CurrentThread.ManagedThreadId;
            }
            else {
                value = eventCache.ThreadId;
            }

            return value;
        }

        private static object FormatTimeStamp(TraceEventCache eventCache) {
            object value;
            if (eventCache == null) {
                value = null;
            }
            else {
                value = eventCache.Timestamp;
            }

            return value;
        }
    }
}