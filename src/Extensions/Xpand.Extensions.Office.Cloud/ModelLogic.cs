using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.Extensions.Office.Cloud{
    public interface IModelReactiveModuleOffice : IModelReactiveModule{
        IModelOffice Office{ get; }
    }

    public interface IModelOffice:IModelNode{
        [Category("User")][RuleRequiredField]
        [DataSourceProperty(nameof(Users))]
        IModelClass User{ get; set; }
        IModelList<IModelClass> Users{ get; }
    }

    [DomainLogic(typeof(IModelOffice))]
    public static class ModelOfficeLogic{

        public static IModelList<IModelClass> Get_Users(this IModelOffice modelOffice){
            return new CalculatedModelNodeList<IModelClass>(modelOffice.Application.BOModel.Where(c =>
                typeof(ISecurityUser).IsAssignableFrom(c.TypeInfo.Type) && !c.TypeInfo.IsAbstract));
        }

        public static IObservable<IModelOffice> OfficeModel(this IObservable<IModelReactiveModules> source){
            return source.Select(modules => modules.Office());
        }

        public static IModelOffice Office(this IModelReactiveModules reactiveModules){
            return ((IModelReactiveModuleOffice) reactiveModules).Office;
        }
    }

}