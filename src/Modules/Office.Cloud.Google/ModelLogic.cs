using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public interface IModelOfficeGoogle : IModelNode{
        IModelGoogle Google{ get; }
    }

    public interface IModelGoogle:IModelNode{
        IModelOAuth OAuth{ get; }
    }

    public static class ModelMicrosoft{
        internal static IModelOAuth OAuthGoogle(this IModelApplication application) =>
            application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().OAuth;
        [PublicAPI]
        public static IObservable<IModelGoogle> GoogletModel(this IObservable<IModelOffice> source) 
            => source.Select(modules => modules.Google());
        public static IModelGoogle Google(this IModelOffice office) 
            => ((IModelOfficeGoogle) office).Google;
    }
    
    
}