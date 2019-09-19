using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Moq;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneMemberValue.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Xpand.XAF.Modules.CloneMemberValue.Tests{
    [Collection(nameof(CloneMemberValueModule))]
    public class CloneMemberValueTests : BaseTest{

        private static CloneMemberValueModule DefaultCloneMemberValueModule(Platform platform,string title){
            var application = platform.NewApplication<CloneMemberValueModule>();
            application.Title = title;
            var cloneMemberValueModule = new CloneMemberValueModule();
            cloneMemberValueModule.AdditionalExportedTypes.AddRange(new[]{typeof(ACmv),typeof(BCmv)});
            application.SetupDefaults(cloneMemberValueModule);
            application.Logon();
            return cloneMemberValueModule;
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Collect_Previous_Current_DetailViews_with_cloneable_members(Platform platform){
            using (var application = DefaultCloneMemberValueModule(platform,nameof(Collect_Previous_Current_DetailViews_with_cloneable_members)).Application){
                
                var modelClass = application.FindModelClass(typeof(ACmv));
                foreach (var modelBOModelClassMember in modelClass.OwnMembers.Cast<IModelMemberCloneValue>()){
                    modelBOModelClassMember.CloneValue = true;
                }

                var detailViews = application.WhenCloneMemberValueDetailViewPairs().Replay();
                using (detailViews.Connect()){
                    var objectSpace1 = application.CreateObjectSpace();
                    var detailView1 = application.CreateDetailView(objectSpace1, objectSpace1.CreateObject<ACmv>());
                    var objectSpace2 = application.CreateObjectSpace();
                    var detailView2 = application.CreateDetailView(objectSpace2, objectSpace2.CreateObject<ACmv>());

                    var viewsTuple = await detailViews.FirstAsync().WithTimeOut();
                    viewsTuple.previous.ShouldBe(detailView1);
                    viewsTuple.current.ShouldBe(detailView2);
                }
            }
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async void Collect_editable_ListViews_with_clonable_members(Platform platform){
            var cloneMemberValueModule = DefaultCloneMemberValueModule(platform,nameof(Collect_editable_ListViews_with_clonable_members));
            var application = cloneMemberValueModule.Application;
            var modelClass = application.FindModelClass(typeof(ACmv));
            foreach (var modelBOModelClassMember in modelClass.OwnMembers.Cast<IModelMemberCloneValue>()){
                modelBOModelClassMember.CloneValue = true;
            }

            var listViews = application.WhenCloneMemberValueListViewCreated().Replay();
            listViews.Connect();

            var modelListView = modelClass.DefaultListView;
            ((IModelListViewNewItemRow) modelListView).NewItemRowPosition=NewItemRowPosition.Top;
            modelListView.AllowEdit = true;

            var collectionSource = application.CreateCollectionSource(application.CreateObjectSpace(),typeof(ACmv),modelListView.Id);
            application.CreateListView(modelListView, collectionSource, true);

            var listView = await listViews.FirstAsync().WithTimeOut();

            listView.Model.ShouldBe(modelListView);                
            application.Dispose();
            
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Collect_ListView_Previous_Current_New_Objects(Platform platform){
            using (var application = DefaultCloneMemberValueModule(platform, nameof(Collect_ListView_Previous_Current_New_Objects)).Application){
                var objectSpace = application.CreateObjectSpace();
                var mock = new Mock<ListEditor>{CallBase = true};
                var aCmv1 = objectSpace.CreateObject<ACmv>();
                var aCmv2 = objectSpace.CreateObject<ACmv>();
                var listEditor = mock.Object;
                var createObjects = listEditor.WhenNewObjectAdding()
                    .FirstAsync().Do(t => t.e.AddedObject = aCmv1)
                    .Merge(listEditor.WhenNewObjectAdding().Skip(1).FirstAsync().Do(t => t.e.AddedObject=aCmv2))
                    .Replay();
                createObjects.Connect();
                var objects = listEditor.NewObjectPairs().Replay();
                objects.Connect();
                listEditor.CallMethod("OnNewObjectAdding");
                listEditor.CallMethod("OnNewObjectAdding");

                var objectPair = await (objects).FirstAsync().WithTimeOut();
                objectPair.previous.ShouldBe(aCmv1);
                objectPair.current.ShouldBe(aCmv2);                
                application.Dispose();
            }
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task CloneMemberValues(Platform platform){
            using (var application = DefaultCloneMemberValueModule(platform, nameof(CloneMemberValues)).Application){
                var objectSpace1 = application.CreateObjectSpace();
                var aCmv1 = objectSpace1.CreateObject<ACmv>();
                aCmv1.PrimitiveProperty = "test";
                var objectSpace2 = application.CreateObjectSpace();
                var aCmv2 = objectSpace2.CreateObject<ACmv>();
                var objectView = application.CreateObjectView<DetailView>(typeof(ACmv));
                ((IModelMemberCloneValue) objectView.Model.ModelClass.FindMember(nameof(ACmv.PrimitiveProperty))).CloneValue=true;

                var clonedMembers = await ((ObjectView)objectView,(object)aCmv1,(object)aCmv2).AsObservable().CloneMembers().WithTimeOut();

                clonedMembers.currentObject.ShouldBe(aCmv2);
                clonedMembers.previousObject.ShouldBe(aCmv1);
                aCmv2.PrimitiveProperty.ShouldBe(aCmv1.PrimitiveProperty);            
                application.Dispose();
            }
        }

    }
}