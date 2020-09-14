using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib.BO;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication{
    public static class CloudService{
        public static IObservable<(string creds, TModelOauth modelOAuth)> ConnectCloudService<TModelOauth>(
            this ApplicationModulesManager manager, string serviceName,string platform, Func<IModelOffice, TModelOauth> oauthFactory) where TModelOauth:IModelOAuth
            => manager.WhenGeneratingModelNodes(application => application.Views)
                .SelectMany(views => {
                    var isWeb = manager.Modules.OfType<AgnosticModule>().First().Name.StartsWith("Web");
                    string parentFolder = null;
                    if (isWeb){
                        parentFolder = "..\\";
                    }
                    foreach (var type in new[]{typeof(Event),typeof(Task)}){
                        ((IModelListView) views[ViewType.ListView.ViewId(type,serviceName)]).DetailView = ((IModelDetailView) views[ViewType.DetailView.ViewId(type,serviceName)]);    
                    }
                    var modelOAuth = oauthFactory(views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office);
                    return Observable.Using(() => File.OpenRead($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\{parentFolder}{serviceName}{platform}AppCredentials.json"),
                            stream => new StreamReader(stream!).ReadToEnd().ReturnObservable()).Select(s => (creds:s,modelOAuth))
                        .Finally(() => {
                            modelOAuth.Prompt=OAuthPrompt.Login;
                        });
                })
                .Merge(manager.WhenApplication(xafApplication => xafApplication.WhenViewOnFrame(typeof(Order))
                    .Do(frame => {
                        var controller = frame.Action("ShowWizard").Controller;
                        controller = null;
                    })).To(default((string creds, TModelOauth modelOAuth))))
                .Merge(manager.WhenCustomizeTypesInfo()
                    .Do(t => {
                        foreach (var type in new[]{typeof(Event),typeof(Task)}){
                            var typeInfo = t.e.TypesInfo.FindTypeInfo(type);
                            typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.ListView, ViewType.ListView.ViewId(type,serviceName)));
                            typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, ViewType.DetailView.ViewId(type,serviceName)));
                        }
                    })
                    .To(default((string creds, TModelOauth modelOAuth))));

        private static string ViewId(this ViewType viewType,Type objectType,string serviceName) => $"{objectType.Name}{serviceName}_{viewType}";
    }
}