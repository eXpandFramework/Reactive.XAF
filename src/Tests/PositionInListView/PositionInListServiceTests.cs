using System.Linq;
using DevExpress.Data;
using DevExpress.Xpo.DB;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.CollectionSource;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.PositionInListview;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
	public class PositionInListServiceTests : PositionInListViewBaseTest{
		[TestCase(null, 1, 2, 3, 4)]
		[TestCase(PositionInListViewNewObjectsStrategy.Last, 1, 2, 3, 4)]
		[TestCase(PositionInListViewNewObjectsStrategy.First, -1, -2, -3, -4)]
		[XpandTest]
		public void When_new_objects_position_them_by_model_strategy(PositionInListViewNewObjectsStrategy? strategy,
			int first, int second, int third, int fourth){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var modelPositionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView;
				var listViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				var modelClass = applicatin.Model.BOModel.GetClass(typeof(PIL));
				listViewItem.ListView = modelClass.DefaultListView;
				if (strategy != null){
					var modelClassItem = modelPositionInListView.ModelClassItems
						.AddNode<IModelPositionInListViewModelClassItem>();
					modelClassItem.ModelClass = modelClass;
					modelClassItem.NewObjectsStrategy = strategy.Value;
				}

				var objectSpace = applicatin.CreateObjectSpace();
				var pil1 = objectSpace.CreateObject<PIL>();
				var pil2 = objectSpace.CreateObject<PIL>();
				var pil3 = objectSpace.CreateObject<PIL>();
				objectSpace.CommitChanges();

				pil1.Order.ShouldBe(first);
				pil2.Order.ShouldBe(second);
				pil3.Order.ShouldBe(third);
				objectSpace = applicatin.CreateObjectSpace();
				var pil4 = objectSpace.CreateObject<PIL>();
				objectSpace.CommitChanges();
				pil4.Order.ShouldBe(fourth);
			}
		}

		[TestCase(SortingDirection.Ascending, 0, 2)]
		[TestCase(SortingDirection.Descending, 2, 0)]
		[XpandTest]
		public void When_listview_creating_sort_collectionsource(SortingDirection sortingDirection, int first, int last){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var positionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				positionInListView.ListView = applicatin.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
				positionInListView.SortingDirection = sortingDirection;
				var listViewColumn = positionInListView.ListView.Columns[nameof(PIL.Name)];
				listViewColumn.SortOrder = 0;
				listViewColumn.SortOrder = ColumnSortOrder.Ascending;
				var objectSpace = applicatin.CreateObjectSpace();
				var pil1 = objectSpace.CreateObject<PIL>();
				pil1.Name = "c";
				var pil2 = objectSpace.CreateObject<PIL>();
				pil2.Name = "a";
				var pil3 = objectSpace.CreateObject<PIL>();
				pil3.Name = "b";
				objectSpace.CommitChanges();

				var listView = applicatin.NewView(positionInListView.ListView).AsListView();
				applicatin.CreateViewWindow().SetView(listView);

				var objects = listView.CollectionSource.Objects<PIL>().Select(pil => pil.Name).ToArray();
				objects[first].ShouldBe(pil1.Name);
				objects[1].ShouldBe(pil2.Name);
				objects[last].ShouldBe(pil3.Name);
			}
		}

		[Test]
		[XpandTest]
		public void When_listview_creating_clear_model_sort(){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var positionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				positionInListView.ListView = applicatin.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
				var listViewColumn = positionInListView.ListView.Columns[nameof(PIL.Name)];
				listViewColumn.SortOrder = ColumnSortOrder.Ascending;

				applicatin.NewView(positionInListView.ListView);

				listViewColumn.SortOrder.ShouldBe(ColumnSortOrder.None);
			}
		}
	}
}