using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using JetBrains.Annotations;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public interface IModelOfficeMicrosoft : IModelNode{
        IModelMicrosoft Microsoft{ get; }
    }

    public interface IModelMicrosoft:IModelNode{
        IModelOAuth OAuth{ get; }
    }

    public interface IModelOAuth:IModelNode{
        [Required][DefaultValue(OAuthPrompt.Consent)]
	    OAuthPrompt Prompt{ get; set; }
        [Description("Space seperated list of scopes")]
        string Scopes{ get; [UsedImplicitly] set; }
        [Required][ModelBrowsable(typeof(DesignerOnlyCalculator))]
        string ClientId{ get; set; }
        [Required]
        string RedirectUri{ get; set; }
        [Required][ModelBrowsable(typeof(DesignerOnlyCalculator))]
        [DefaultValue("Applicable only for web")]
        string ClientSecret{ get; set; }
    }
    
    [DomainLogic(typeof(IModelOAuth))]
    public static class ModelOathLogic{
	    internal static IModelOAuth OAuth(this IModelApplication application) =>
		    application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().OAuth;

	    internal static string[] Scopes(this IModelOAuth modelOAuth) =>
		    $"{modelOAuth.Scopes}".Split(' ').Add("User.Read").Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
    }
    
    public enum OAuthPrompt{
	    [SuppressMessage("ReSharper", "InconsistentNaming")] [UsedImplicitly]
        Select_Account,
        Login,
        Consent
    }

    public static class ModelMicrosoft{
        [PublicAPI]
        public static IObservable<IModelMicrosoft> MicrosoftModel(this IObservable<IModelOffice> source) => source.Select(modules => modules.Microsoft());
        public static IModelMicrosoft Microsoft(this IModelOffice office) => ((IModelOfficeMicrosoft) office).Microsoft;
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