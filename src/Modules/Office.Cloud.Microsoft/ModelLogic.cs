using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public interface IModelOfficeMicrosoft : IModelNode{
        IModelMicrosoft Microsoft{ get; }
    }

    public interface IModelMicrosoft:IModelNode{
        IModelOAuthRedirectUri OAuth{ get; }
    }

    public interface IModelOAuthRedirectUri:IModelOAuth{
        [Required]
        string RedirectUri{ get; set; }
    }
    public static class ModelMicrosoft{
        internal static IModelOAuthRedirectUri OAuthMS(this IModelApplication application) 
            => application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().OAuth;
        [PublicAPI]
        public static IObservable<IModelMicrosoft> MicrosoftModel(this IObservable<IModelOffice> source) 
            => source.Select(modules => modules.Microsoft());
        public static IModelMicrosoft Microsoft(this IModelOffice office) 
            => ((IModelOfficeMicrosoft) office).Microsoft;
    }
    
    public interface IModelCloudItem{
        [Required][DefaultValue(SynchronizationType.All)]
        SynchronizationType SynchronizationType{ get; [UsedImplicitly] set; }
    }

    public interface IModelCallDirection{
        [Required][DefaultValue(CallDirection.Both)]
        CallDirection CallDirection{ get; set; }
    }
    public interface IModelSynchronizationType{
        [Required][DefaultValue(SynchronizationType.All)]
        SynchronizationType SynchronizationType{ get; set; }
    }

    public enum CallDirection{
        Both,
        In,
        Out
    }
}