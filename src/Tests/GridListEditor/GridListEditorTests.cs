using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.XtraGrid;
using Fasterflect;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.GridListEditor.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using ListView = DevExpress.ExpressApp.ListView;


namespace Xpand.XAF.Modules.GridListEditor.Tests{
    [NonParallelizable]
    public class GridListEditorTests : BaseTest{
        [Test]
        [XpandTest]
        [Ignore("Random fail on azure")]
        [Apartment(ApartmentState.STA)]
        public async Task Remember_TopRowIndex_WHen_Refresh_View_DataSource(){
            
            var application = GridListEditorModule().Application;
            
            var items = application.Model.ToReactiveModule<IModelReactiveModuleGridListEditor>().GridListEditor;
            var topRow = items.GridListEditorRules.AddNode<IModelGridListEditorTopRow>();
            topRow.ListView = application.Model.BOModel.GetClass(typeof(GLE)).DefaultListView;
            var objectSpace = application.CreateObjectSpace();
            
            for (int i = 0; i < 2; i++){
                var gle = objectSpace.CreateObject<GLE>();
                gle.Age = i;
            }
            objectSpace.CommitChanges();
            var whenViewOnFrame = application.WhenViewOnFrame().Publish().RefCount();
            var topRowIndex = whenViewOnFrame
                .SelectMany(frame => {
                    var viewObjectSpace = frame.View.ObjectSpace;
                    for (int i = 0; i < 100; i++){
                        viewObjectSpace.CreateObject<GLE>();
                    }
                    viewObjectSpace.CommitChanges();
                    var reload = ((ListView) frame.View).CollectionSource.WhenCollectionReloaded().SubscribeReplay(1);
                    frame.View.RefreshDataSource();
                    return reload.Select(_ => ((DevExpress.ExpressApp.Win.Editors.GridListEditor) ((ListView) frame.View).Editor).GridView.TopRowIndex);
                })
                .FirstAsync()
                .Do(_ => application.Exit())
                .SubscribeReplay();
            
                            
                
            ((TestWinApplication) application).Start();
            
            var rowIndex = await topRowIndex;
            rowIndex.ShouldBe(0);
        }

        [TestCase(true)]
        [TestCase(false)]
        [XpandTest][Apartment(ApartmentState.STA)]
        public void FocusRow(bool upArrowMoveToRowHandle){
            
            var application = GridListEditorModule().Application;
            var objectSpace = application.CreateObjectSpace();
            objectSpace.CreateObject<GLE>();
            objectSpace.CommitChanges();
            var items = application.Model.ToReactiveModule<IModelReactiveModuleGridListEditor>().GridListEditor;
            var focusRow = items.GridListEditorRules.AddNode<IModelGridListEditorFocusRow>();
            var modelClass = application.Model.BOModel.GetClass(typeof(GLE));
            ((IModelClassShowAutoFilterRow) modelClass).DefaultListViewShowAutoFilterRow = true;
            focusRow.ListView = modelClass.DefaultListView;
            focusRow.RowHandle = focusRow.RowHandles.First(s => s==nameof(GridControl.AutoFilterRowHandle));
            if (upArrowMoveToRowHandle) {
                focusRow.UpArrowMoveToRowHandle = focusRow.RowHandle;
                focusRow.RowHandle = null;
            }
        
            var testObserver = application.WhenFrameViewChanged().WhenFrame(ViewType.ListView)
                .Select(frame => {
                    var gridView = ((DevExpress.ExpressApp.Win.Editors.GridListEditor) frame.View.AsListView().Editor).GridView;
                    if (upArrowMoveToRowHandle) {
	                    gridView.FocusedRowHandle = 0;
                        gridView.CallMethod("RaiseKeyDown", new KeyEventArgs(Keys.Up));
                    }
                    return gridView.FocusedRowHandle;
                })
                .Do(_ => application.Exit())
                .Test();
        
        
            ((TestWinApplication) application).Start();
        
            testObserver.AssertValues(GridControl.AutoFilterRowHandle);
        }

        private static GridListEditorModule GridListEditorModule(Platform platform=Platform.Win){
            var application = platform.NewApplication<GridListEditorModule>();
            application.EditorFactory=new EditorsFactory();
            return application.AddModule<GridListEditorModule>(typeof(GLE));
        }
    }
}