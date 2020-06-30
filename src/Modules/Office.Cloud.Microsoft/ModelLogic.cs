using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Office.Cloud;

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
        string Scopes{ get; set; }
    }

    public enum OAuthPrompt{
		None,    
        [SuppressMessage("ReSharper", "InconsistentNaming")] 
        Select_Account,
        Login,
        Consent
    }

    public static class ModelMicrosoft{
        public static IObservable<IModelMicrosoft> MicrosoftModel(this IObservable<IModelOffice> source) => source.Select(modules => modules.Microsoft());
        public static IModelMicrosoft Microsoft(this IModelOffice office) => ((IModelOfficeMicrosoft) office).Microsoft;
    }
}