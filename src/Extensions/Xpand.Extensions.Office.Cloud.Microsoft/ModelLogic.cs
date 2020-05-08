using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.Office.Cloud.Microsoft{
    public interface IModelOfficeMicrosoft : IModelNode{
        IModelMicrosoft Microsoft{ get; }
    }

    public interface IModelMicrosoft:IModelNode{
        
    }

    public static class ModelMicrosoft{
        public static IObservable<IModelOfficeMicrosoft> MicrosoftModel(this IObservable<IModelReactiveModuleOffice> source){
            return source.Select(modules => modules.Microsoft());
        }

        public static IModelOfficeMicrosoft Microsoft(this IModelReactiveModuleOffice reactiveModules){
            return (IModelOfficeMicrosoft) reactiveModules.Office;
        }
        public static IModelMicrosoft Microsoft(this IModelOffice office){
            return ((IModelOfficeMicrosoft) office).Microsoft;
        }
    }
}