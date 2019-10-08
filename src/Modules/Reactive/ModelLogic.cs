using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive{
    public interface IModelApplicationReactiveModules:IModelNode{
        IModelReactiveModules ReactiveModules{ get; }
    }

    public interface IModelReactiveModules : IModelNode{
    }
    public interface IModelReactiveModule : IModelNode{
    }

    public static class ReactiveModulesExtension{
        public static IObservable<IModelReactiveModules> ReactiveModulesModel(this XafApplication application){
            return application.ReactiveModule(() => ((IModelApplicationReactiveModules) application.Model).ReactiveModules);
        }

        public static IObservable<TModel> ToReactiveModule<TModel>(this XafApplication application) where TModel:IModelReactiveModule{
            return application.ReactiveModule(() => application.Model.ToReactiveModule<TModel>());
        }

        private static IObservable<T> ReactiveModule<T>(this XafApplication application,Func<T> model) {
            var applicationModel = (bool) application.GetFieldValue("isLoggedOn");
            return applicationModel
                ? model().AsObservable()
                : application.WhenLoggedOn().Select(_ => model());
        }

        public static TModel ToReactiveModule<TModel>(this IModelApplication applicationModel) where TModel:IModelReactiveModule{
            return ((TModel) ((IModelApplicationReactiveModules) applicationModel).ReactiveModules);
        }
    }
}