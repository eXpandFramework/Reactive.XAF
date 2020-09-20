using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
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

    [PublicAPI]
    public interface IModelCalendar:IModelNode{
        [Required]
        string DefaultCalendarName{ get; set; }
        [DataSourceProperty(nameof(NewCloudEvents))]
        [Required]
        IModelClass NewCloudEvent{ get; set; }
        [Browsable(false)]
        IModelList<IModelClass> NewCloudEvents{ get; }

        IModelCalendarItems Items{ get; }
    }

    [DomainLogic(typeof(IModelCalendar))]
    public static class ModelCalendarLogic{
        [PublicAPI]
        public static string Get_DefaultCalendarName(this IModelCalendar modelCalendar){
            var interfaces = modelCalendar.Parent.GetType().GetInterfaces();
            if (interfaces.Any(type => type.Name.Contains("Microsoft"))){
                return "Calendar";
            }
            if (interfaces.Any(type => type.Name.Contains("Google"))){
                return "primary";
            }
            throw new NotImplementedException();
        }

        public static IModelClass Get_NewCloudEvent(this IModelCalendar modelCalendar)
            => modelCalendar.NewCloudEvents.FirstOrDefault(); 
        public static CalculatedModelNodeList<IModelClass> Get_NewCloudEvents(this IModelCalendar modelCalendar) 
            => modelCalendar.Application.BOModel.Where(c =>c.TypeInfo.IsPersistent&&!c.TypeInfo.IsAbstract&&typeof(IEvent).IsAssignableFrom(c.TypeInfo.Type) ).ToCalculatedModelNodeList();
    }

    public interface IModelCalendarItems : IModelList<IModelCalendarItem>,IModelNode{
    }

    public interface IModelCalendarItem:IModelSynchronizationType,IModelCallDirection,IModelObjectViewDependency{
    }

    public interface IModelCallDirection{
        [Required][DefaultValue(CallDirection.Both)]
        CallDirection CallDirection{ get; set; }
    }
    
    public enum CallDirection{
        Both,
        In,
        Out
    }
}