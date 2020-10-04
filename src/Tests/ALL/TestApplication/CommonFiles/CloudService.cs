using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Diagnostics;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace TestApplication{
    public static class CloudService{
        public static IObservable<Unit> PushTheToken<TObject>(this ApplicationModulesManager manager,string serviceName,Func<TObject,string> tokenFactory) where TObject:CloudOfficeBaseObject
            => manager.RegisterViewSimpleAction($"Push{serviceName}Token",typeof(PermissionPolicyUser),ViewType.DetailView)
                .ActivateInUserDetails().FirstAsync()
                .WhenExecute(e => {
                    using var objectSpace = e.Action.Application.CreateObjectSpace();
                    var authentication = objectSpace.GetObjectByKey<TObject>(SecuritySystem.CurrentUserId);
                    
                    var token = tokenFactory(authentication);

                    var fullPath = Path.GetFullPath($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\PushToken.ps1");
                    File.WriteAllText($"{Path.GetDirectoryName(fullPath)}\\{serviceName}Token.txt",token);
                    var processStartInfo = new ProcessStartInfo("powershell.exe",$@"""{fullPath}"" '{serviceName}' -SkipPushToken"){WorkingDirectory = Directory.GetCurrentDirectory()};
                    var process = new Process(){StartInfo = processStartInfo};
                    process.StartWithEvents(createNoWindow:false);
                    var output = process.WhenOutputDataReceived().Buffer(process.WhenExited).WhenNotEmpty().Do(t => {
                        Tracing.Tracer.LogSeparator("PushToken");
                        Tracing.Tracer.LogText(t.Join(Environment.NewLine));
                    }).Publish();
                    output.Connect();
                    process.WaitForExit();
                    return output.ToUnit();
                });

        public static IObservable<(string creds, TModelOauth modelOAuth)> ConnectCloudService<TModelOauth>(
            this ApplicationModulesManager manager, string serviceName,string platform, Func<IModelOffice, TModelOauth> oauthFactory) where TModelOauth:IModelOAuth
            => manager.UpdateCloudModel( serviceName, platform, oauthFactory)
                .Merge(manager.GenerateCloudViews<TModelOauth>(serviceName));


        private static IObservable<(string creds, TModelOauth modelOAuth)> GenerateCloudViews<TModelOauth>(
            this ApplicationModulesManager manager, string serviceName) where TModelOauth : IModelOAuth 
            => manager.WhenCustomizeTypesInfo()
                .Do(t => {
                    foreach (var type in new[]{typeof(Event),typeof(Task)}){
                        var typeInfo = t.e.TypesInfo.FindTypeInfo(type);
                        typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.ListView, ViewType.ListView.ViewId(type,serviceName)));
                        typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, ViewType.DetailView.ViewId(type,serviceName)));
                    }
                })
                .To(default((string creds, TModelOauth modelOAuth)));

        private static IObservable<(string creds, TModelOauth modelOAuth)> UpdateCloudModel<TModelOauth>(
            this ApplicationModulesManager manager, string serviceName, string platform,
            Func<IModelOffice, TModelOauth> oauthFactory) where TModelOauth : IModelOAuth 
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
                });

        private static string ViewId(this ViewType viewType,Type objectType,string serviceName) 
            => $"{objectType.Name}{serviceName}_{viewType}";

        public static IObservable<Unit> DeleteAllEntities<TLocalEntity>(this XafApplication application, IObservable<IObservable<Unit>> deleteAll)
            => deleteAll.Switch().ToUnit()
                .Merge(application.WhenWindowCreated().When(TemplateContext.ApplicationWindow).FirstAsync()
                    .Do(window => {
                        using var objectSpace = window.Application.CreateObjectSpace();
                        objectSpace.Delete(objectSpace.GetObjects<TLocalEntity>());
                        objectSpace.CommitChanges();
                    }).ToUnit());
    }
}