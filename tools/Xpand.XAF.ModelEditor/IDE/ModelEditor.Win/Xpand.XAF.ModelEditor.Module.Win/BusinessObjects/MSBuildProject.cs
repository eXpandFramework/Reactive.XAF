using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace Xpand.XAF.ModelEditor.Module.Win.BusinessObjects {
    [DefaultClassOptions]
    public class MSBuildProject:XPBaseObject {
        public MSBuildProject(Session session) : base(session){
        }

        int _index;

        [Browsable(false)][Key(true)]
        public int Index{
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
        string _path;
        [Browsable(false)][Size(-1)]
        public string Path {
            get => _path;
            set => SetPropertyValue(nameof(Path),ref _path, value);
        }

        string _targetFramework;

        public string TargetFramework{
            get => _targetFramework;
            set => SetPropertyValue(nameof(TargetFramework), ref _targetFramework, value);
        }

        bool _appendTargetFramework;

        public bool AppendTargetFramework{
            get => _appendTargetFramework;
            set => SetPropertyValue(nameof(AppendTargetFramework), ref _appendTargetFramework, value);
        }

        string _outputPath;

        public string OutputPath{
            get => _outputPath;
            set => SetPropertyValue(nameof(OutputPath), ref _outputPath, value);
        }

        bool _isApplicationProject;

        public bool IsApplicationProject{
            get => _isApplicationProject;
            set => SetPropertyValue(nameof(IsApplicationProject), ref _isApplicationProject, value);
        }

        string _assemblyPath;

        public string AssemblyPath{
            get => _assemblyPath;
            set => SetPropertyValue(nameof(AssemblyPath), ref _assemblyPath, value);
        }

        string _targetFileName;

        public string TargetFileName{
            get => _targetFileName;
            set => SetPropertyValue(nameof(TargetFileName), ref _targetFileName, value);
        }

        string _cannotRunMEMessage;

        [NonPersistent]
        public string CannotRunMEMessage{
            get => _cannotRunMEMessage;
            set => SetPropertyValue(nameof(CannotRunMEMessage), ref _cannotRunMEMessage, value);
        }
    }
}