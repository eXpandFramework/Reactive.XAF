using System.Linq;
using DevExpress.Data;
using DevExpress.Xpo.DB;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
	public class PositionInListServiceTests : PositionInListViewCommonTest{

		[TestCase(null, 1, 2, 3, 4)]
		[TestCase(PositionInListViewNewObjectsStrategy.Last, 1, 2, 3, 4)]
		[TestCase(PositionInListViewNewObjectsStrategy.First, -1, -2, -3, -4)]
		[XpandTest]
		public void When_new_objects_position_them_by_model_strategy(PositionInListViewNewObjectsStrategy? strategy,
			int first, int second, int third, int fourth) {
            using var application = PositionInListViewModuleModule().Application;
            var modelPositionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView;
            var listViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
            var modelClass = application.Model.BOModel.GetClass(typeof(PIL));
            listViewItem.ListView = modelClass.DefaultListView;
            if (strategy != null){
                var modelClassItem = modelPositionInListView.ModelClassItems
                    .AddNode<IModelPositionInListViewModelClassItem>();
                modelClassItem.ModelClass = modelClass;
                modelClassItem.NewObjectsStrategy = strategy.Value;
            }

            var objectSpace = application.CreateObjectSpace();
            var pil1 = objectSpace.CreateObject<PIL>();
            var pil2 = objectSpace.CreateObject<PIL>();
            var pil3 = objectSpace.CreateObject<PIL>();
            objectSpace.CommitChanges();	

            pil1.Order.ShouldBe(first);
            pil2.Order.ShouldBe(second);
            pil3.Order.ShouldBe(third);
            
            objectSpace = application.CreateObjectSpace();
            var pil4 = objectSpace.CreateObject<PIL>();
            objectSpace.CommitChanges();
            pil4.Order.ShouldBe(fourth);
        }

		[TestCase(SortingDirection.Descending, 2, 0)]
		[TestCase(SortingDirection.Ascending, 0, 2)]
		[XpandTest]
		public void When_ListView_Creating_Sort_CollectionSource(SortingDirection sortingDirection, int first, int last) {
            using var application = PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
            positionInListView.ListView = application.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
            positionInListView.SortingDirection = sortingDirection;
            var listViewColumn = positionInListView.ListView.Columns[nameof(PIL.Name)];
            listViewColumn.SortOrder = 0;
            listViewColumn.SortOrder = ColumnSortOrder.Ascending;
            var objectSpace = application.CreateObjectSpace();
            var pil1 = objectSpace.CreateObject<PIL>();
            pil1.Name = "c";
            objectSpace.CommitChanges();
            var pil2 = objectSpace.CreateObject<PIL>();
            pil2.Name = "a";
            objectSpace.CommitChanges();
            var pil3 = objectSpace.CreateObject<PIL>();
            pil3.Name = "b";
            objectSpace.CommitChanges();
            

            var listView = application.NewView(positionInListView.ListView).AsListView();
            application.CreateViewWindow().SetView(listView);

            var objects = listView.CollectionSource.Objects<PIL>().Select(pil => pil.Name).ToArray();
            objects[first].ShouldBe(pil1.Name);
            objects[1].ShouldBe(pil2.Name);
            objects[last].ShouldBe(pil3.Name);
        }

	}
}