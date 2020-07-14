using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.Extensions.Office.Cloud{
    public interface IModelReactiveModuleOffice : IModelReactiveModule{
        IModelOffice Office{ get; }
    }

    public interface IModelOffice:IModelNode{
        
    }

    [DomainLogic(typeof(IModelOffice))]
    public static class ModelOfficeLogic{
        
        public static IObservable<IModelOffice> OfficeModel(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.Office());
        }

        public static IModelOffice Office(this IModelReactiveModules reactiveModules){
            return ((IModelReactiveModuleOffice) reactiveModules).Office;
        }
    }

}