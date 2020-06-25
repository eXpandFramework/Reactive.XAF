using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.Office.Cloud.Microsoft{
    public interface IModelOfficeMicrosoft : IModelNode{
        IModelMicrosoft Microsoft{ get; }
    }

    public interface IModelMicrosoft:IModelNode{
	    [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.ImageGalleryModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, typeof(UITypeEditor))]
	    [Category("Appearance")][DefaultValue(nameof(ConnectImageName))][Required]
	    string ConnectImageName { get; set; }
	    [Category("Appearance")][DefaultValue(nameof(DisconnectImageName))][Required]
	    [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.ImageGalleryModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, typeof(UITypeEditor))]
	    string DisconnectImageName { get; set; }
    }

    public static class ModelMicrosoft{
        public static IObservable<IModelMicrosoft> MicrosoftModel(this IObservable<IModelOffice> source) => source.Select(modules => modules.Microsoft());

        

        public static IModelMicrosoft Microsoft(this IModelOffice office) => ((IModelOfficeMicrosoft) office).Microsoft;
    }
}