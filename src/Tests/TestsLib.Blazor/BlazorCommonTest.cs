using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            WebHost?.Dispose();
        }

        protected BlazorApplication NewBlazorApplication(Type startupType){
            IHostBuilder defaultBuilder = Host.CreateDefaultBuilder();
            
            WebHost = defaultBuilder
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup(startupType))
                .Build();
            WebHost.Start();
            var containerInitializer = WebHost.Services.GetService<IValueManagerStorageContainerInitializer>();
            if (((IValueManagerStorageAccessor) containerInitializer)?.Storage == null) {
                containerInitializer.Initialize();
            }
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