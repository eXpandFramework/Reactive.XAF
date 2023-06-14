using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;

namespace Xpand.XAF.Modules.MasterDetail{
    public interface IModelApplicationMasterDetail{
        IModelDashboardMasterDetail DashboardMasterDetail{ get; }
    }
    [ModelAbstractClass]
    public interface IModelDashboardViewMasterDetail : IModelDashboardView {
        [Category(ReactiveMasterDetailModule.CategoryName)]
        [ModelBrowsable(typeof(ModelDashboardViewMasterDetailVisibilityCalculator))]
        bool MasterDetail { get; set; }


        [Category(ReactiveMasterDetailModule.CategoryName)]
        [ModelBrowsable(typeof(ModelDashboardViewMasterDetailVisibilityCalculator))]
        IModelMasterDetailDetailViewObjectTypeLinks MasterDetailDetailViewObjectTypeLinks { get; }
    }

    [DomainLogic(typeof(IModelDashboardViewMasterDetail))]
    public class ModelDashboardViewMasterDetailDomainLogic {
        public static bool Get_MasterDetail(IModelDashboardViewMasterDetail dashboardViewMasterDetail) {
            return new ModelDashboardViewMasterDetailVisibilityCalculator().IsVisible(dashboardViewMasterDetail, null);
        }
    }

    public interface IModelDashboardMasterDetail:IModelNode{
        IModelMasterDetailDetailViewObjectTypeLinks ObjectTypeLinks{ get; }
    }

    [ModelNodesGenerator(typeof(MasterDetailViewObjectTypeLinkNodesGenerator))]
    public interface IModelMasterDetailDetailViewObjectTypeLinks : IModelNode, IModelList<IModelMasterDetailViewObjectTypeLink> {
    }

    public interface IModelMasterDetailViewObjectTypeLink:IModelNode{
        [Required]
        [DataSourceProperty("Application.Views")]
        [DataSourceCriteria("(AsObjectView Is Not Null) And (AsObjectView.ModelClass Is Not Null) And (IsAssignableFromViewModelClass('@This.TypeInfo', AsObjectView))")]
        IModelDetailView DetailView { get; set; }
        [DataSourceProperty("Application.BOModel")]
        [Required]
        IModelClass ModelClass { get; set; }
        [CriteriaOptions("TypeInfo")]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        string Criteria{ get; set; }
        [Browsable(false)]
        ITypeInfo TypeInfo { get; }
    }

    [DomainLogic(typeof(IModelMasterDetailViewObjectTypeLink))]
    public class ModelMasterDetailViewObjectTypeLinkLogic{
        public static ITypeInfo Get_TypeInfo(IModelMasterDetailViewObjectTypeLink link){
            return link.ModelClass?.TypeInfo;
        }

    }

    public class MasterDetailViewObjectTypeLinkNodesGenerator : ModelNodesGeneratorBase {
        protected override void GenerateNodesCore(ModelNode node) {

        }
    }

    public class ModelDashboardViewMasterDetailVisibilityCalculator : IModelIsVisible {
        public bool IsVisible(IModelNode node, string propertyName) {
            var viewItems = ((IModelDashboardViewMasterDetail)node).Items.OfType<IModelDashboardViewItem>().ToArray();
            var modelObjectViews = viewItems.Select(item => item.View).OfType<IModelObjectView>().ToArray();
            return modelObjectViews.Length == 2 && modelObjectViews.Length == viewItems.Length &&
                   modelObjectViews.GroupBy(view => view.ModelClass).Count() == 1&&modelObjectViews.First().GetType()!=modelObjectViews.Last().GetType();
        }
    }
}