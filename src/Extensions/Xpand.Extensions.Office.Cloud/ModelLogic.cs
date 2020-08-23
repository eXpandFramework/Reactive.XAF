using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;
using Xpand.Extensions.LinqExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.Extensions.Office.Cloud{
    public interface IModelReactiveModuleOffice : IModelReactiveModule{
        IModelOffice Office{ get; }
    }

    public interface IModelOffice:IModelNode{
        
    }

    [DomainLogic(typeof(IModelOffice))]
    public static class ModelOfficeLogic{
        [PublicAPI]
        public static IObservable<IModelOffice> Office(this IObservable<IModelReactiveModules> source) 
            => source.Select(modules => modules.Office());

        public static IModelOffice Office(this IModelReactiveModules reactiveModules) 
            => ((IModelReactiveModuleOffice) reactiveModules).Office;
    }

    public enum OAuthPrompt{
        [SuppressMessage("ReSharper", "InconsistentNaming")] [UsedImplicitly]
        Select_Account,
        Login,
        Consent
    }

    public interface IModelOAuthRedirectUri:IModelOAuth{
        [Required]
        string RedirectUri{ get; set; }
    }

    public interface IModelOAuth:IModelNode{
        [Required][DefaultValue(OAuthPrompt.Consent)]
        OAuthPrompt Prompt{ get; [UsedImplicitly] set; }
        [Description("Space seperated list of scopes")]
        string Scopes{ get; [UsedImplicitly] set; }
        [Required]
        string ClientId{ get; set; }
        [Required]
        string ClientSecret{ get; set; }
    }
    
    [DomainLogic(typeof(IModelOAuth))]
    public static class ModelOathLogic{
        public static void AddScopes(this IModelOAuth modelOAuth, params string[] scopes) 
            => modelOAuth.Scopes = modelOAuth.Scopes().Concat(scopes).Distinct().Join(" ");

        internal static string[] Scopes(this IModelOAuth modelOAuth) 
            => $"{modelOAuth.Scopes}".Split(' ').Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
    }

    public interface IModelSynchronizationType{
        [Required][DefaultValue(SynchronizationType.All)]
        SynchronizationType SynchronizationType{ get; [UsedImplicitly] set; }
    }
}