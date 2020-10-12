using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.XtraRichEdit.API.Native;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager{
    public static class ShowService{
        
        public static SingleChoiceAction ShowStyleManager(this (DocumentStyleManagerModule, Frame frame) tuple) => tuple.frame.Action(nameof(ShowStyleManager)).As<SingleChoiceAction>();

        internal static IObservable<Unit> ShowStyleManager(this ApplicationModulesManager manager){
            var registerViewSingleChoiceAction = manager.RegisterViewSingleChoiceAction(nameof(ShowStyleManager),
                action => {
                    action.Caption = "Style Manager";
                    action.TargetViewType=ViewType.DetailView;
                    action.ItemType=SingleChoiceActionItemType.ItemIsOperation;
                    action.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
                    action.Active[nameof(ShowService)] = false;    
                },PredefinedCategory.Tools);
            var registerShowStyleManagerAction = registerViewSingleChoiceAction
                .WhenExecute()
                .SelectMany(args => args.Action.Application.DefaultPropertiesProvider(document => {
	                args.ShowStyleManagerOnExecute(document);
	                return Unit.Default.ReturnObservable();
                }));
            var configureAction = manager
		            .WhenApplication(application => application.WhenViewOnFrame()
			            .Do(frame => frame.Action<DocumentStyleManagerModule>().ShowStyleManager()?.ConfigureShowStyleManagerAction(frame.View.Model))
			            .ToUnit());
            return registerShowStyleManagerAction.Merge(configureAction);
        }

        private static void ConfigureShowStyleManagerAction(this SingleChoiceAction showStyleManagerAction,IModelView modelView){
            var items = modelView.Application.DocumentStyleManager().DesignTemplateDetailViews.Where(view => view.DetailView==modelView)
                .SelectMany(view => view.ContentEditors).Select(editor => editor.ContentEditor).ToArray();
            showStyleManagerAction.Items.Clear();
            showStyleManagerAction.Items.AddRange(items.Cast<IModelViewItem>()
                .Select(manager => new ChoiceActionItem(manager.Id, manager.Caption, manager)).ToArray());
            showStyleManagerAction.Active[nameof(ShowService)] = items.Any();
        }

        private static void ShowStyleManagerOnExecute(this SingleChoiceActionExecuteEventArgs e, Document defaultPropertiesProvider){
            var application = e.Action.Application;
            var view = e.Action.View();
            var showViewParameters = e.ShowViewParameters;
            var objectSpace = application.CreateObjectSpace(typeof(BusinessObjects.DocumentStyleManager));
            var documentStyleManager = objectSpace.CreateObject<BusinessObjects.DocumentStyleManager>();
            var memberInfo = ((IModelPropertyEditor) e.SelectedChoiceActionItem.Data).ModelMember.MemberInfo;
            documentStyleManager.Content = (byte[]) memberInfo.GetValue(view.CurrentObject);
            documentStyleManager.Original = (byte[]) memberInfo.GetValue(view.CurrentObject);
            
            documentStyleManager.SynchronizeStyles(defaultPropertiesProvider);
            showViewParameters.Controllers.Add(new ContentInfoController(memberInfo,view.CurrentObject));

            showViewParameters.CreatedView = application.CreateDetailView(objectSpace, documentStyleManager);
            showViewParameters.TargetWindow=TargetWindow.NewModalWindow;
        }

    }
    class ContentInfoController : Controller{
        public ContentInfoController(IMemberInfo memberInfo,object callerObject){
            MemberInfo = memberInfo;
            CallerObject = callerObject;
        }

        public IMemberInfo MemberInfo{ get;  }
        public object CallerObject{ get; }
    }

}