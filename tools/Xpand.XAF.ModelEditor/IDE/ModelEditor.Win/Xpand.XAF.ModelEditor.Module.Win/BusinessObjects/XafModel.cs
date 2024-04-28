using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace Xpand.XAF.ModelEditor.Module.Win.BusinessObjects {
    [DefaultClassOptions]
    [System.ComponentModel.DisplayName("")]
    public class XafModel:XPBaseObject {
        public XafModel(Session session) : base(session){
        }

        int _index;

        [Browsable(false)][Key(true)]
        public int Index{
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }
        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(nameof(Name),ref _name, value);
        }

        MSBuildProject _mSBuildProject;
        [Browsable(false)]
        public MSBuildProject Project {
            get => _mSBuildProject;
            set => SetPropertyValue(nameof(Project),ref _mSBuildProject, value);
        }

        string _path;
        [Browsable(false)] 
        public string Path{
            get => _path;
            set => SetPropertyValue(nameof(Path), ref _path, value);
        }

        bool _debug;

        public bool Debug{
            get => _debug;
            set => SetPropertyValue(nameof(Debug), ref _debug, value);
        }
    }
}