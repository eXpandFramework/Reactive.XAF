using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.ApplyTemplateStyle{
	public class ShowServiceTests:Tests.BaseTests{
		[Test][XpandTest()]
		public void Action_is_Active_only_for_ModelTemplateListViews(){
			using var application=DocumentStyleManagerModule().Application;
			var window = application.CreateViewWindow();
			window.SetView(application.NewView(ViewType.ListView, typeof(DataObject)));

			Services.StyleTemplateService.ShowService.ShowApplyStylesTemplate(window.Action<DocumentStyleManagerModule>()).Active[nameof(ShowService)].ShouldBeFalse();

			var item = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule.ApplyTemplateListViews.AddNode<IModelApplyTemplateListViewItem>();
			item.ListView = application.Model.BOModel.GetClass(typeof(DataObject)).DefaultListView;

			window.SetView(application.NewView(ViewType.ListView, typeof(DataObject)));

			Services.StyleTemplateService.ShowService.ShowApplyStylesTemplate(window.Action<DocumentStyleManagerModule>()).Active[nameof(ShowService)].ShouldBeTrue();
		}

		[Test][Apartment(ApartmentState.STA)][XpandTest()]
		public void Action_Shows_ApplyTemplateStyle_DetailView_and_initialize_template(){
			using var application=DocumentStyleManagerModule().Application;
			var window = application.CreateViewWindow();
			var item = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule.ApplyTemplateListViews.AddNode<IModelApplyTemplateListViewItem>();
			item.ListView = application.Model.BOModel.GetClass(typeof(DataObject)).DefaultListView;
			window.SetView(application.NewView(ViewType.ListView, typeof(DataObject)));
			var action = Services.StyleTemplateService.ShowService.ShowApplyStylesTemplate(window.Action<DocumentStyleManagerModule>());
			action.WhenExecuted().FirstAsync()
				.Do(e => e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow).Test();
			var testObserver = application.WhenViewOnFrame(typeof(Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.ApplyTemplateStyle), ViewType.DetailView).Test();
			var dataObject = window.View.ObjectSpace.CreateObject<DataObject>();
			dataObject.Content=new byte[]{1};
			dataObject.Name = nameof(DataObject);
			dataObject.ObjectSpace.CommitChanges();

			action.DoExecute(space => new object[]{dataObject});

			testObserver.ItemCount.ShouldBe(1);
			var applyTemplateStyle = ((Xpand.XAF.Modules.Office.DocumentStyleManager.BusinessObjects.ApplyTemplateStyle) testObserver.Items.First().View.CurrentObject);
			applyTemplateStyle.ListView.ShouldBe(item.ListView.Id);
			var documents = applyTemplateStyle.Documents;
			documents.Count.ShouldBe(1);
			documents.Select(document => document.Key).First().ShouldBe(dataObject.Oid);
			documents.Select(document => document.Name).First().ShouldBe(dataObject.Name);
			
		}


	}
}
