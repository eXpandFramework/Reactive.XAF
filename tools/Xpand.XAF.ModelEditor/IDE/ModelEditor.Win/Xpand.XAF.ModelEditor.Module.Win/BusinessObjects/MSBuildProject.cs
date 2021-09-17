using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.ModelEditor.Module.Win.BusinessObjects {
    [DomainComponent][DefaultClassOptions]
    public class MSBuildProject:NonPersistentBaseObject {
        string _path;
        [Browsable(false)]
        public string Path {
            get => _path;
            set => SetPropertyValue(ref _path, value);
        }

        string _targetFramework;

        public string TargetFramework {
            get => _targetFramework;
            set => SetPropertyValue(ref _targetFramework, value);
        }

        bool _appendTargetFramework;

        public bool AppendTargetFramework {
            get => _appendTargetFramework;
            set => SetPropertyValue(ref _appendTargetFramework, value);
        }

        string _outputPath;

        public string OutputPath {
            get => _outputPath;
            set => SetPropertyValue(ref _outputPath, value);
        }

        
        public bool IsApplicationProject { get; set; }
        public string AssemblyPath { get; set; }
        public string TargetFileName { get; set; }
        public string CannotRunMEMessage { get; set; }
    }
}