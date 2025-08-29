using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Xpand.Extensions.Tracing {
    class RollingTextWriter(string filePathTemplate) : IDisposable {
        const int MaxStreamRetries = 5;

        private string _currentPath;
        private TextWriter _currentWriter;
        private readonly Lock _fileLock = new();
        private IFileSystem _fileSystem = new FileSystem();
        readonly TraceFormatter _traceFormatter = new();

        public static RollingTextWriter Create(string filePathTemplate) {
            var segments = filePathTemplate.Split('%');
            if (segments.Length > 3) {
                throw new ArgumentException("InitializeData should contain maximum 1 environment variable.",
                    nameof(filePathTemplate));
            }

            if (segments.Length == 3) {
                var variableName = segments[1];
                var rootFolder = Environment.GetEnvironmentVariable(variableName);
                if (String.IsNullOrEmpty(rootFolder)) {
                    if (variableName.Equals("ProgramData", StringComparison.CurrentCultureIgnoreCase) &&
                        (Environment.OSVersion.Version.Major <=
                         5)) {
                        rootFolder = Path.Combine(Environment.GetEnvironmentVariable("AllUsersProfile")!,
                            "Application Data");
                    }
                    else {
                        throw new ArgumentException("Environment variable is not recognized in InitializeData.",
                            nameof(filePathTemplate));
                    }
                }

                var filePath = rootFolder + segments[2];
                return new RollingTextWriter(filePath);
            }

            return new RollingTextWriter(filePathTemplate);
        }

        public string FilePathTemplate { get; } = filePathTemplate;

        public IFileSystem FileSystem {
            get => _fileSystem;
            set {
                lock (_fileLock) {
                    _fileSystem = value;
                }
            }
        }

        public void Flush() {
            lock (_fileLock) {
                _currentWriter?.Flush();
            }
        }

        public void Write(TraceEventCache eventCache, string value) {
            string filePath = GetCurrentFilePath(eventCache);
            lock (_fileLock) {
                EnsureCurrentWriter(filePath);
                _currentWriter.Write(value);
            }
        }

        public void WriteLine(TraceEventCache eventCache, string value) {
            string filePath = GetCurrentFilePath(eventCache);
            lock (_fileLock) {
                EnsureCurrentWriter(filePath);
                _currentWriter.WriteLine(value);
            }
        }

        private void EnsureCurrentWriter(string path) {
            if (_currentPath != path) {
                if (_currentWriter != null) {
                    _currentWriter.Close();
                    _currentWriter.Dispose();
                    _currentWriter = null;
                    _currentPath = null;
                }

                var num = 0;
                var stream = default(Stream);

                while (stream == null && num < MaxStreamRetries) {
                    var fullPath = num == 0 ? path : GetFullPath(path, num);
                    try {
                        stream = FileSystem.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);

                        _currentWriter = new StreamWriter(stream);
                        _currentPath = path;

                        return;
                    }
                    catch (DirectoryNotFoundException) {
                        throw;
                    }
                    catch (IOException) { }

                    num++;
                }

                throw new InvalidOperationException("Exhausted possible logfile names");
            }
        }

        static string GetFullPath(string path, int num) {
            var extension = Path.GetExtension(path);
            return path.Insert(path.Length - extension.Length, "-" + num.ToString(CultureInfo.InvariantCulture));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework",
            MessageId = "System.DateTimeOffset", Justification = "Deliberate dependency, .NET 2.0 SP1 required.")]
        private string GetCurrentFilePath(TraceEventCache eventCache) {
            var result = StringTemplate.Format(CultureInfo.CurrentCulture, FilePathTemplate,
                delegate(string name, out object value) {
                    switch (name.ToUpperInvariant()) {
                        case "ACTIVITYID":
                            value = Trace.CorrelationManager.ActivityId;
                            break;
                        case "APPDATA":
                            value = _traceFormatter.HttpTraceContext.AppDataPath;
                            break;
                        case "APPDOMAIN":
                            value = AppDomain.CurrentDomain.FriendlyName;
                            break;
                        case "APPLICATIONNAME":
                            value = _traceFormatter.FormatApplicationName();
                            break;
                        case "DATETIME":
                        case "UTCDATETIME":
                            value = TraceFormatter.FormatUniversalTime(eventCache);
                            break;
                        case "LOCALDATETIME":
                            value = TraceFormatter.FormatLocalTime(eventCache);
                            break;
                        case "MACHINENAME":
                            value = Environment.MachineName;
                            break;
                        case "PROCESSID":
                            value = _traceFormatter.FormatProcessId(eventCache);
                            break;
                        case "PROCESSNAME":
                            value = _traceFormatter.FormatProcessName();
                            break;
                        case "USER":
                            value = Environment.UserDomainName + "-" + Environment.UserName;
                            break;
                        default:
                            value = "{" + name + "}";
                            break;
                    }

                    return true;
                });
            return result;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _currentWriter?.Dispose();
            }
        }
    }
}