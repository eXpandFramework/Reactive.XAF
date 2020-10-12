using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using ALL.Tests;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.AuditTrail;
using DevExpress.ExpressApp.Chart;
using DevExpress.ExpressApp.CloneObject;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Kpi;
using DevExpress.ExpressApp.Notifications;
using DevExpress.ExpressApp.Objects;
using DevExpress.ExpressApp.PivotChart;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.Scheduler;
using DevExpress.ExpressApp.ScriptRecorder;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Xpo;
using DevExpress.ExpressApp.StateMachine;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.TreeListEditors;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.ViewVariantsModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Newtonsoft.Json;
using TestApplication.GoogleService;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.WindowManager;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.BO;
using Xpand.XAF.Modules.AutoCommit;
using Xpand.XAF.Modules.CloneMemberValue;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.HideToolBar;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelViewInheritance;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Office.Cloud.Google.Calendar;
using Xpand.XAF.Modules.Office.Cloud.Google.Tasks;
#if !NETCOREAPP3_1
using TestApplication.MicrosoftCalendarService;
using TestApplication.MicrosoftService;
using TestApplication.MicrosoftTodoService;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo;    
using Xpand.XAF.Modules.ProgressBarViewItem;
using Xpand.XAF.Modules.Office.DocumentStyleManager;
#endif

using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Xpand.XAF.Modules.RefreshView;
using Xpand.XAF.Modules.SequenceGenerator;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xpand.XAF.Modules.ViewEditMode;
using Xpand.XAF.Modules.ViewItemValue;
using Xpand.XAF.Modules.ViewWizard;

namespace TestApplication{
    public static class AgnosticExtensions{
        public static void ConfigureConnectionString(this XafApplication application){
            application.ConnectionString = InMemoryDataStoreProvider.ConnectionString;
            var easyTestSettingsFile = AppDomain.CurrentDomain.EasyTestSettingsFile();
            if (File.Exists(easyTestSettingsFile)){
                var settings = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(easyTestSettingsFile));
                application.ConnectionString = settings.ConnectionString;
            }
        }

        public static string EasyTestSettingsFile(this AppDomain appDomain) 
			=>appDomain.IsHosted()? $"{appDomain.ApplicationPath()}\\..\\EasyTestSettings.json":$"{appDomain.ApplicationPath()}\\EasyTestSettings.json";
    }
	public abstract class AgnosticModule:ModuleBase{
		protected AgnosticModule(){
			#region XAF Modules

			RequiredModuleTypes.Add(typeof(AuditTrailModule));
			RequiredModuleTypes.Add(typeof(ChartModule));
			RequiredModuleTypes.Add(typeof(CloneObjectModule));
			RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule));
			RequiredModuleTypes.Add(typeof(DashboardsModule));
			RequiredModuleTypes.Add(typeof(KpiModule));
			RequiredModuleTypes.Add(typeof(NotificationsModule));
			RequiredModuleTypes.Add(typeof(BusinessClassLibraryCustomizationModule));
			RequiredModuleTypes.Add(typeof(PivotChartModuleBase));
			RequiredModuleTypes.Add(typeof(PivotGridModule));
			RequiredModuleTypes.Add(typeof(ReportsModuleV2));
			RequiredModuleTypes.Add(typeof(SchedulerModuleBase));
			RequiredModuleTypes.Add(typeof(ScriptRecorderModuleBase));
			RequiredModuleTypes.Add(typeof(SecurityModule));
			RequiredModuleTypes.Add(typeof(SecurityXpoModule));
			RequiredModuleTypes.Add(typeof(StateMachineModule));
			RequiredModuleTypes.Add(typeof(TreeListEditorsModuleBase));
			RequiredModuleTypes.Add(typeof(SystemModule));
			RequiredModuleTypes.Add(typeof(ValidationModule));
			RequiredModuleTypes.Add(typeof(ViewVariantsModule));
			// RequiredModuleTypes.Add(typeof(WorkflowModule));
			// RequiredModuleTypes.Add(typeof(ServerUpdateDatabaseModule));

			#endregion

            AdditionalExportedTypes.Add(typeof(Order));

			RequiredModuleTypes.Add(typeof(AutoCommitModule));
			RequiredModuleTypes.Add(typeof(CloneMemberValueModule));
			RequiredModuleTypes.Add(typeof(CloneModelViewModule));
			RequiredModuleTypes.Add(typeof(HideToolBarModule));
			RequiredModuleTypes.Add(typeof(MasterDetailModule));
			
			RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
         RequiredModuleTypes.Add(typeof(ModelMapperModule));	
            if (!Debugger.IsAttached){
                // RequiredModuleTypes.Add(typeof(ModelMapperModule));	
            }
#if !NETCOREAPP3_1
            
         RequiredModuleTypes.Add(typeof(MicrosoftCalendarModule));
         RequiredModuleTypes.Add(typeof(MicrosoftTodoModule));
			RequiredModuleTypes.Add(typeof(ProgressBarViewItemModule));
#endif
			
			RequiredModuleTypes.Add(typeof(ReactiveModule));
			RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
			RequiredModuleTypes.Add(typeof(RefreshViewModule));
			RequiredModuleTypes.Add(typeof(SequenceGeneratorModule));
			RequiredModuleTypes.Add(typeof(SuppressConfirmationModule));
			RequiredModuleTypes.Add(typeof(ViewEditModeModule));
#if !XAF191 && !NETCOREAPP3_1
            RequiredModuleTypes.Add(typeof(DocumentStyleManagerModule));
#endif
			
			RequiredModuleTypes.Add(typeof(ViewItemValueModule));
			RequiredModuleTypes.Add(typeof(GoogleModule));
			RequiredModuleTypes.Add(typeof(GoogleTasksModule));
			RequiredModuleTypes.Add(typeof(GoogleCalendarModule));
			RequiredModuleTypes.Add(typeof(ReactiveLoggerHubModule));
			RequiredModuleTypes.Add(typeof(ViewWizardModule));
			AdditionalExportedTypes.Add(typeof(Event));
			AdditionalExportedTypes.Add(typeof(Task));
		}

		public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB){
			base.GetModuleUpdaters(objectSpace, versionFromDB);
			yield return new DefaultUserModuleUpdater(objectSpace, versionFromDB,Guid.Parse("5c50f5c6-e697-4e9e-ac1b-969eac1237f3"),true);
      }

		public override void Setup(XafApplication application){
			base.Setup(application);
			application.Security = new SecurityStrategyComplex(typeof(PermissionPolicyUser),
				typeof(PermissionPolicyRole), new AuthenticationStandard(typeof(PermissionPolicyUser), typeof(AuthenticationStandardLogonParameters)));
		}

		public override void Setup(ApplicationModulesManager moduleManager){
			base.Setup(moduleManager);
            moduleManager.ConnectGoogleTasksService()
#if !NETCOREAPP3_1
                .Merge(moduleManager.ConnectMicrosoftTodoService())
                .Merge(moduleManager.ConnectMicrosoftService())
                .Merge(moduleManager.ConnectMicrosoftCalendarService())
#endif
                .Merge(moduleManager.ConnectGoogleCalendarService())
                .Merge(moduleManager.ConnectCloudCalendarService())
                .Merge(moduleManager.ConnectGoogleService())
                .Merge(moduleManager.ConnectViewWizardService())
                // .Merge(moduleManager.WhenApplication(xafApplication => xafApplication.WhenFrameCreated()
		              //   .SelectMany(frame => frame.WhenViewChanging().Where(t => t.args.View is DetailView)))
	               //  .Select(t => {
		              //   var controller1 = t.frame.GetController("ShowStyleManagerActionViewController");
		              //   Interaction.MsgBox(controller1?.Name);
		              //   return controller1;
	               //  }).WhenNotDefault()
	               //  .SelectMany(t => {
		              //   
		              //   return t.WhenActivated().Do(controller => {
			             //    Interaction.MsgBox("a");
		              //   });
	               //  }).ToUnit())
                .Subscribe(this);
        }
	}
}