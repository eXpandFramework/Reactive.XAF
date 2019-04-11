//using System;
//using System.Linq;
//using DevExpress.ExpressApp;
//using DevExpress.ExpressApp.Actions;
//using DevExpress.Persistent.Base;
//using Xpand.Source.Extensions.XAF.XafApplication;
//
//namespace Xpand.XAF.Modules.MasterDetail{
//    class MasterDetailActionsController : ViewController<DetailView> {
//        public const string ActiveKey = "OnlyInDashboard";
//        public const string ActionEnabledKey = "CurrentObject is not null";
//        public SimpleAction MasterDetailSaveAction { get; }
//        public MasterDetailActionsController() {
//            MasterDetailSaveAction = new SimpleAction(this, "MasterDetailSaveAction", PredefinedCategory.Edit.ToString(),
//                (s, e) => { ObjectSpace.CommitChanges(); }) {
//                Caption = "Save",
//                ImageName = "MenuBar_Save"
//            };
//            Active[ActiveKey] = false;
//            MasterDetailSaveAction.Enabled[ActionEnabledKey] = false;
//        }
//
//        protected override void OnActivated() {
//            base.OnActivated();
//            if (Application.GetPlatform()==Platform.Web)
//                Frame.Controllers.Cast<Controller>().First(controller => controller.GetType().Name == "ActionsFastCallbackHandlerController").Active[GetType().FullName] = false;
//            View.CurrentObjectChanged += ViewOnCurrentObjectChanged;
//        }
//
//        protected override void OnViewControlsCreated() {
//            base.OnViewControlsCreated();
//            UpdateActionState();
//        }
//
//        protected override void OnDeactivated() {
//            base.OnDeactivated();
//            View.CurrentObjectChanged -= ViewOnCurrentObjectChanged;
//        }
//
//        private void ViewOnCurrentObjectChanged(object sender, EventArgs eventArgs) {
//            UpdateActionState();
//        }
//
//        private void UpdateActionState() {
//            MasterDetailSaveAction.Enabled[ActionEnabledKey] = View.CurrentObject != null;
//        }
//    }
//}