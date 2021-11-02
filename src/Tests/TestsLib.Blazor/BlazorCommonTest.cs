using System;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.AmbientContext;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Blazor {
    public class BlazorCommonTest:CommonTest {
        protected IHost WebHost;


        static BlazorCommonTest() {
            TestsLib.Common.Extensions.ApplicationType = typeof(TestBlazorApplication);
        }
        public override void Dispose() {
            base.Dispose();
            CleanBlazorEnviroment();
        }

        protected void CleanBlazorEnviroment() {
            WebHost?.Dispose();
            typeof(ValueManagerContext).Field("storageHolder", Flags.StaticPrivate).SetValue(null,
                typeof(AsyncLocal<>).MakeGenericType(AppDomain.CurrentDomain.GetAssemblyType(
                    "DevExpress.ExpressApp.Blazor.AmbientContext.ValueManagerContext+StorageHolder")).CreateInstance());
        }

        protected BlazorApplication NewBlazorApplication(Type startupType){
            var defaultBuilder = Host.CreateDefaultBuilder();
            
            WebHost = defaultBuilder
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup(startupType))
                .Build();
            WebHost.Start();
            var containerInitializer = WebHost.Services.GetRequiredService<IValueManagerStorageContainerInitializer>();
            containerInitializer.Initialize();
            var newBlazorApplication = WebHost.Services.GetService<IXafApplicationProvider>()?.GetApplication();
            if (newBlazorApplication != null) {
                newBlazorApplication.ServiceProvider = WebHost.Services;
            }

            newBlazorApplication.WhenApplicationModulesManager().FirstAsync()
	            .SelectMany(manager => manager.WhenGeneratingModelNodes<IModelViews>().FirstAsync()
		            .SelectMany().OfType<IModelListView>().Where(view => view.EditorType==typeof(GridListEditor))
		            .Do(view => view.DataAccessMode = CollectionSourceDataAccessMode.Client))
	            .Subscribe();
            return newBlazorApplication;
        }

    }
}