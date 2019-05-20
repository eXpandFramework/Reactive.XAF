using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AppDomainToolkit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Moq;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.CloneMemberValue.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneMemberValue;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Tests.Modules.CloneMemberValue{
//    [Collection(nameof(XafTypesInfo))]
    public class CloneMemberValueTests : BaseTest{

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Configure_which_members_are_cloned_from_the_model(Platform platform){
            RemoteFunc.Invoke(Domain, platform, _ => {
                var cloneMemberValueModule = DefaultCloneMemberValueModule(_);
                var modelClass = cloneMemberValueModule.Application.Model.BOModel.GetClass(typeof(ACmv));
                foreach (var member in modelClass.OwnMembers.Cast<IModelMemberCloneValue>().Skip(1)){
                    member.CloneValue = true;
                }
            
                modelClass.DefaultDetailView.CloneValueMemberViewItems().Count().ShouldBe(1);
                return Unit.Default;
            });
            
        }

        private static CloneMemberValueModule DefaultCloneMemberValueModule(Platform platform){
            var application = platform.NewApplication();
            var cloneMemberValueModule = new CloneMemberValueModule();
            cloneMemberValueModule.AdditionalExportedTypes.AddRange(new[]{typeof(ACmv),typeof(BCmv)});
            application.SetupDefaults(cloneMemberValueModule);
            return cloneMemberValueModule;
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Collect_Previous_Current_DetailViews_with_cloneable_members(Platform platform){
            await RemoteFuncAsync.InvokeAsync(Domain, platform, async _ => {
                var cloneMemberValueModule = DefaultCloneMemberValueModule(_);
                var application = cloneMemberValueModule.Application;
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
                return Unit.Default;
            }).WithTimeOut();
            
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async void Collect_editable_ListViews_with_clonable_members(Platform platform){
            await RemoteFuncAsync.InvokeAsync(Domain, platform, async _ => {
                var cloneMemberValueModule = DefaultCloneMemberValueModule(_);
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
                return Unit.Default;
            }).WithTimeOut();
            
            
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Collect_ListView_Previous_Current_New_Objects(Platform platform){
            await RemoteFuncAsync.InvokeAsync(Domain, platform, async _ => {
                var application = DefaultCloneMemberValueModule(_).Application;
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
                return Unit.Default;
            }).WithTimeOut();
            
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task CloneMemberValues(Platform platform){
            await RemoteFuncAsync.InvokeAsync(Domain, platform, async _ => {
                var cloneMemberValueModule = DefaultCloneMemberValueModule(_);
                var application = cloneMemberValueModule.Application;
                var objectSpace1 = application.CreateObjectSpace();
                var aCmv1 = objectSpace1.CreateObject<ACmv>();
                aCmv1.PrimitiveProperty = "test";
                var objectSpace2 = application.CreateObjectSpace();
                var aCmv2 = objectSpace2.CreateObject<ACmv>();
                var modelObjectView = application.FindModelClass(typeof(ACmv)).DefaultDetailView.AsObjectView;
                ((IModelMemberCloneValue) modelObjectView.ModelClass.FindMember(nameof(ACmv.PrimitiveProperty))).CloneValue=true;

                var clonedMembers = await (modelObjectView,(IObjectSpaceLink)aCmv1,(IObjectSpaceLink)aCmv2).AsObservable().CloneMembers().WithTimeOut();

                clonedMembers.currentObject.ShouldBe(aCmv2);
                clonedMembers.previousObject.ShouldBe(aCmv1);
                aCmv2.PrimitiveProperty.ShouldBe(aCmv1.PrimitiveProperty);
                return Unit.Default;
            }).WithTimeOut();
            
        }

    }
}