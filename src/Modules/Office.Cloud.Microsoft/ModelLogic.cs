using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Office.Cloud;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public interface IModelOfficeMicrosoft : IModelNode{
        IModelMicrosoft Microsoft{ get; }
    }

    public interface IModelMicrosoft:IModelNode{
    }

    public static class ModelMicrosoft{
        public static IObservable<IModelMicrosoft> MicrosoftModel(this IObservable<IModelOffice> source) => source.Select(modules => modules.Microsoft());
        public static IModelMicrosoft Microsoft(this IModelOffice office) => ((IModelOfficeMicrosoft) office).Microsoft;
    }
}