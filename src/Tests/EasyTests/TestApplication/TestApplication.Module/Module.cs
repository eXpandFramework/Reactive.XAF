using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using ALL.Tests;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart;
using DevExpress.ExpressApp.CloneObject;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Kpi;
using DevExpress.ExpressApp.Notifications;
using DevExpress.ExpressApp.Objects;
using DevExpress.ExpressApp.PivotChart;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.ScriptRecorder;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.StateMachine;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.TreeListEditors;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.ViewVariantsModule;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using TestApplication.GoogleService;
using TestApplication.Module.ViewWizard;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.BO;
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
using Xpand.XAF.Modules.ProgressBarViewItem;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.RefreshView;
using Xpand.XAF.Modules.SequenceGenerator;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xpand.XAF.Modules.ViewEditMode;
using Xpand.XAF.Modules.ViewItemValue;
using Xpand.XAF.Modules.ViewWizard;

namespace TestApplication.Module {
    public interface IWebModule {
        string Name { get; }
    }
    public sealed class TestApplicationModule : ModuleBase {
        public TestApplicationModule() {
            			#region XAF Modules

			// RequiredModuleTypes.Add(typeof(AuditTrailModule));
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
            RequiredModuleTypes.Add(typeof(ScriptRecorderModuleBase));
			RequiredModuleTypes.Add(typeof(SecurityModule));
            RequiredModuleTypes.Add(typeof(StateMachineModule));
			RequiredModuleTypes.Add(typeof(TreeListEditorsModuleBase));
			RequiredModuleTypes.Add(typeof(SystemModule));
			RequiredModuleTypes.Add(typeof(ValidationModule));
			RequiredModuleTypes.Add(typeof(ViewVariantsModule));

            #endregion

            AdditionalExportedTypes.Add(typeof(Order));

            RequiredModuleTypes.Add(typeof(AutoCommitModule));
            RequiredModuleTypes.Add(typeof(CloneMemberValueModule));
            RequiredModuleTypes.Add(typeof(CloneModelViewModule));
            RequiredModuleTypes.Add(typeof(HideToolBarModule));
            RequiredModuleTypes.Add(typeof(MasterDetailModule));
            RequiredModuleTypes.Add(typeof(ModelViewInheritanceModule));
            RequiredModuleTypes.Add(typeof(ProgressBarViewItemModule));
            RequiredModuleTypes.Add(typeof(ModelMapperModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
			
			RequiredModuleTypes.Add(typeof(RefreshViewModule));
			RequiredModuleTypes.Add(typeof(SequenceGeneratorModule));
			RequiredModuleTypes.Add(typeof(SuppressConfirmationModule));
			RequiredModuleTypes.Add(typeof(ViewEditModeModule));
            RequiredModuleTypes.Add(typeof(ViewItemValueModule));
			RequiredModuleTypes.Add(typeof(GoogleModule));
			RequiredModuleTypes.Add(typeof(GoogleTasksModule));
			RequiredModuleTypes.Add(typeof(GoogleCalendarModule));
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
			// RequiredModuleTypes.Add(typeof(ReactiveLoggerHubModule));
			RequiredModuleTypes.Add(typeof(ViewWizardModule));
			AdditionalExportedTypes.Add(typeof(Event));
			AdditionalExportedTypes.Add(typeof(Task));

        }

        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDb){
            base.GetModuleUpdaters(objectSpace, versionFromDb);
            yield return new DefaultUserModuleUpdater(objectSpace, versionFromDb,Guid.Parse("5c50f5c6-e697-4e9e-ac1b-969eac1237f3"),true);
            yield return new OrderModuleUpdater(objectSpace, versionFromDb);
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.ConnectViewWizardService()
                .Merge(moduleManager.ConnectGoogleCalendarService())
                .Merge(moduleManager.ConnectCloudCalendarService())
                .Merge(moduleManager.ConnectGoogleService())
                .Merge(moduleManager.ConnectGoogleTasksService())
                .Subscribe(this);
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo) {
            base.CustomizeTypesInfo(typesInfo);
            CalculatedPersistentAliasHelper.CustomizeTypesInfo(typesInfo);
        }
    }
}
