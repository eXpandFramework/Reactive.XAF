using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.DC;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TestApplication.Blazor.Server.BusinessObjects;
using Xpand.XAF.Modules.Blazor;

[assembly: HostingStartup(typeof(Xpand.Extensions.Blazor.HostingStartup))]
[assembly:HostingStartup(typeof(BlazorStartup))]
namespace Xpand.XAF.Modules.Email.Tests.Common {
    class TestModule:ModuleBase {
        public TestModule(){
            RequiredModuleTypes.Add(typeof(BlazorModule));
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            ((TypeInfo)typesInfo.FindTypeInfo(typeof(ApplicationUser))).CreateMember("Email",typeof(string));
        }
    }
    public class TestStartup(IConfiguration configuration) : TestApplication.Blazor.Server.Startup(configuration) {
        
        protected override void AddModules(IBlazorApplicationBuilder builder) {
            base.AddModules(builder);
            builder.Modules.Add<EmailModule>();
            builder.Modules.Add<TestModule>();
        }

        
    }
}