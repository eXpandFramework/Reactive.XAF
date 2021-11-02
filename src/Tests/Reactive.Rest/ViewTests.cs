using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Rest.Tests.BO;
using Xpand.XAF.Modules.Reactive.Rest.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Tests {
	public class ViewTests : RestCommonAppTest {
		[Test][Order(100)]
		public async Task TestListViewProcessSelectedItem() {
			HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)),
				o => o.StringArray = new[] {"a"});
			
			await Application.TestListViewProcessSelectedItem(typeof(RestPropertyObject));
		}

		[Test][Order(0)]
		public async Task Arrays_BindingList_Lookup_Datasource() {
			HandlerMock.SetupRestPropertyObject(Application.CreateObjectSpace(typeof(RestPropertyObject)),
				o => o.StringArray = new[] {"a"});
			var detailView = await Application.TestListViewProcessSelectedItem(typeof(RestPropertyObject));
			var nestedFrame = detailView.GetListPropertyEditor<RestPropertyObject>(o => o.StringArrayList).Frame;
			var whenDetailViewCreated =
				Application.WhenDetailViewCreated(typeof(ObjectString)).FirstAsync().SubscribeReplay();
			var newObjectAction = nestedFrame.GetController<NewObjectViewController>().NewObjectAction;
			newObjectAction.DoExecute(
				space => new[] {space.GetObject(nestedFrame.View.AsListView().CollectionSource.Objects().First())},
				true);

			var t = await whenDetailViewCreated;

			var currentObject = ((ObjectString) t.e.View.CurrentObject);
			var restPropertyObject = ((RestPropertyObject) detailView.CurrentObject);
			currentObject.DataSource.Count.ShouldBe(restPropertyObject.StringArraySource.Count);
			currentObject.DataSource.Select(s => s.Name).First()
				.ShouldBe(restPropertyObject.StringArraySource.Select(s => s.Name).First());
		}
	}
}