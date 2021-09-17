using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.ModelEditor.Module.Win.BusinessObjects {
    [DomainComponent][DefaultClassOptions]
    [DisplayName("")]
    public class XafModel:NonPersistentBaseObject {
        

        string _name;

        public string Name {
            get => _name;
            set => SetPropertyValue(ref _name, value);
        }

        MSBuildProject _mSBuildProject;
        [Browsable(false)]
        public MSBuildProject Project {
            get => _mSBuildProject;
            set => SetPropertyValue(ref _mSBuildProject, value);
        }

        [Browsable(false)]
        public string Path { get; set; }

        bool _debug;

        public bool Debug {
            get => _debug;
            set => SetPropertyValue(ref _debug, value);
        }
    }
}