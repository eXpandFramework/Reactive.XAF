using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.GridListEditor.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.GridListEditor.Tests{
    [NonParallelizable]
    public class GridListEditorTests : BaseTest{
        [Test]
        [XpandTimeout]
        [Apartment(ApartmentState.STA)]
        public async Task Remember_WHen_Refresh_View_DataSource(){
            
            var application = GridListEditorModule(nameof(Remember_WHen_Refresh_View_DataSource)).Application;
            
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



        private static GridListEditorModule GridListEditorModule(string title,Platform platform=Platform.Win){
            var application = platform.NewApplication<GridListEditorModule>();
            application.EditorFactory=new EditorsFactory();
            return application.AddModule<GridListEditorModule>(title,typeof(GLE));
        }
    }
}