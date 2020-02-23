using System;
using DevExpress.Persistent.Base;

namespace Xpand.XAF.ModelEditor {
    public class PathInfo {
        public PathInfo(string[] args) {
            Tracing.Tracer.LogValue("PathInfo", args);
            try {
                AssemblyPath = args[1].TrimStart(Convert.ToChar("\"")).TrimEnd(Convert.ToChar("\""));
                FullPath = args[3].TrimStart(Convert.ToChar("\"")).TrimEnd(Convert.ToChar("\""));
                LocalPath = args[2].TrimStart(Convert.ToChar("\"")).TrimEnd(Convert.ToChar("\""));
                IsApplicationModel = Convert.ToBoolean(args[0].Trim());
            }
            catch (IndexOutOfRangeException) {
                var errorText = "Needs params: " + Environment.NewLine +
                                "    1) d - show GetFileName of ApplicationBase (may be empty)" + Environment.NewLine +
                                "    2) true|false - is exe(web)|dll " + Environment.NewLine +
                                "    3) module dll full path" + Environment.NewLine +
                                "    4) Model.xafml full path" + Environment.NewLine +
                                "    5) Model.xafml folder path" + Environment.NewLine +
                                "Input params: " + Environment.NewLine +
                                $"    1) {(args.Length == 1 ? args[0] : "null") }" + Environment.NewLine +
                                $"    2) {(args.Length == 2 ? args[1] : "null") }" + Environment.NewLine +
                                $"    3) {(args.Length == 3 ? args[2] : "null") }" + Environment.NewLine +
                                $"    4) {(args.Length == 4 ? args[3] : "null") }" + Environment.NewLine +
                                $"    5) {(args.Length == 5 ? args[4] : "null") }";
                throw new ArgumentException(errorText);
            }
        }

        public bool IsApplicationModel{ get; }

        public override string ToString() {
            return
                $"AssemblyPath={AssemblyPath}{Environment.NewLine}FullPath={FullPath}{Environment.NewLine}LocalPath={LocalPath}";
        }

        public string AssemblyPath { get; set; }

        public string FullPath{ get; }

        public string LocalPath{ get; }
    }
}