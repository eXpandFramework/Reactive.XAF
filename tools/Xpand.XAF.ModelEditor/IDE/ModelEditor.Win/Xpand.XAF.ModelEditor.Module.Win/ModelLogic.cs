using System.ComponentModel;
using DevExpress.ExpressApp.Model;

namespace Xpand.XAF.ModelEditor.Module.Win {
    public interface IModelApplicationME:IModelNode {
        IModelME ModelEditor { get; }
    }

    public interface IModelME:IModelNode {
        [Required]
        [DefaultValue("https://github.com/eXpandFramework/Reactive.XAF/releases/download/{0}/Xpand.XAF.ModelEditor.{1}.Zip")]
        string DownloadUrl { get; set; }

        bool DownloadPreRelease { get; set; }
    }
}
