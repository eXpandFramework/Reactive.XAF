using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Moq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.CloneMemberValue.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;


namespace Xpand.XAF.Modules.CloneMemberValue.Tests{
    [NonParallelizable]
    public class CloneMemberValueTests : BaseTest{

        private static CloneMemberValueModule DefaultCloneMemberValueModule(string title){
            var application = Platform.Win.NewApplication<CloneMemberValueModule>();
            application.Title = title;
            var cloneMemberValueModule = new CloneMemberValueModule();
            cloneMemberValueModule.AdditionalExportedTypes.AddRange(new[]{typeof(ACmv),typeof(BCmv)});
            application.SetupDefaults(cloneMemberValueModule);
            application.Logon();
            return cloneMemberValueModule;
        }

        [XpandTest]
        [Test()]
        public async Task Collect_Previous_Current_DetailViews_with_cloneable_members(){
            using var application = DefaultCloneMemberValueModule(nameof(Collect_Previous_Current_DetailViews_with_cloneable_members)).Application;
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

        [XpandTest]
        [Test()]
        public async Task Collect_editable_ListViews_with_clonable_members(){
            var cloneMemberValueModule = DefaultCloneMemberValueModule(nameof(Collect_editable_ListViews_with_clonable_members));
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
        [XpandTest]
        [Test()]
        public async Task Collect_ListView_Previous_Current_New_Objects(){
            using var application = DefaultCloneMemberValueModule( nameof(Collect_ListView_Previous_Current_New_Objects)).Application;
            var objectSpace = application.CreateObjectSpace();
            var mock = new Mock<ListEditor>{CallBase = true};
            var aCmv1 = objectSpace.CreateObject<ACmv>();
            var aCmv2 = objectSpace.CreateObject<ACmv>();
            var listEditor = mock.Object;
            var createObjects = listEditor.WhenNewObjectAdding()
                .FirstAsync().Do(e => {
                    if (e != null) e.AddedObject = aCmv1;
                })
                .Merge(listEditor.WhenNewObjectAdding().Skip(1).FirstAsync().Do(e => {
                    if (e != null) e.AddedObject = aCmv2;
                }))
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
        [XpandTest]
        [Test()]
        public async Task CloneMemberValues(){
            using var application = DefaultCloneMemberValueModule(nameof(CloneMemberValues)).Application;
            var objectSpace1 = application.CreateObjectSpace();
            var aCmv1 = objectSpace1.CreateObject<ACmv>();
            aCmv1.PrimitiveProperty = "test";
            var objectSpace2 = application.CreateObjectSpace();
            var aCmv2 = objectSpace2.CreateObject<ACmv>();
            var objectView = application.NewObjectView<DetailView>(typeof(ACmv));
            ((IModelMemberCloneValue) objectView.Model.ModelClass.FindMember(nameof(ACmv.PrimitiveProperty))).CloneValue=true;

            var clonedMembers = await ((ObjectView)objectView,(object)aCmv1,(object)aCmv2).Observe().CloneMembers().WithTimeOut();

            clonedMembers.currentObject.ShouldBe(aCmv2);
            clonedMembers.previousObject.ShouldBe(aCmv1);
            aCmv2.PrimitiveProperty.ShouldBe(aCmv1.PrimitiveProperty);            
            application.Dispose();
        }

    }
}