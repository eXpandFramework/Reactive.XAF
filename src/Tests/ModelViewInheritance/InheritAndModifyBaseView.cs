using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.ModelViewInheritance.Tests.BOModel;

namespace Xpand.XAF.Modules.ModelViewInheritance.Tests{
    class InheritAndModifyBaseView{
        private readonly ViewType _viewType;

        public InheritAndModifyBaseView(ViewType viewType) => _viewType = viewType;

        public void Verify(IModelApplication modelApplication){
            var modelClassB = modelApplication.BOModel.GetClass(typeof(AMvi));
            var viewB =_viewType==ViewType.ListView? modelClassB.DefaultListView.AsObjectView:modelClassB.DefaultDetailView;    

            viewB.Caption.ShouldBe("Changed");
            viewB.AllowDelete.ShouldBe(false);
            if (viewB is IModelListView modelListView) {
                modelListView.Columns[nameof(ABaseMvi.Description)].Caption.ShouldBe("New");
                modelListView.Columns[nameof(ABaseMvi.Name)].ShouldBeNull();
                modelListView.Columns[nameof(ABaseMvi.Oid)].Index.ShouldBe(100);
                ((IModelViewHiddenActions) modelListView).HiddenActions.Any().ShouldBeTrue();
            }
            else {
                var modelDetailView = ((IModelDetailView) viewB);
                modelDetailView.Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(AMvi)}/{nameof(ABaseMvi.Name)}").ShouldBeNull();
                modelDetailView.Layout.GetNodeByPath($"Main/SimpleEditors/{nameof(ABaseMvi)}/{nameof(ABaseMvi.Description)}").ShouldNotBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Oid").ShouldNotBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Tags_Groups").ShouldBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Tabs/FileMvis/FileMvis").ShouldNotBeNull();
                modelDetailView.Layout.GetNodeByPath("Main/Tabs/Tags/Tags");
                modelDetailView.Layout.GetNode("Main").NodeCount.ShouldBe(3);
            }

        }
    }
}