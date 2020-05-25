using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Model;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupCascade.Tests{
    public class ModelLogicTests:LookupCascadeBaseTest{
        [XpandTest][Order(0)]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        public void Cascade_And_Synchronize_MemberViewItems_Lookups_Contain_members_that_use_the_Editor(string viewId){
            var application = ClientLookupCascadeModule().Application;
            var applicationModel = application.Model;
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            var productModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Product));
            productModelMemberViewItem.PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
            var accesoryModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Accessory));
            accesoryModelMemberViewItem.PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
        
            var productPropertyEditor = ((IModelMemberViewItemLookupCascadePropertyEditor) productModelMemberViewItem).LookupCascade;
            productPropertyEditor.LookupPropertyEditorMemberViewItems.ShouldContain(modelMemberViewItems.First(item => item.Id==nameof(Order.Accessory)));
            productPropertyEditor.LookupPropertyEditorMemberViewItems.Count().ShouldBe(1);
            
            var orderPropertyEditor = ((IModelMemberViewItemLookupCascadePropertyEditor) accesoryModelMemberViewItem).LookupCascade;
            orderPropertyEditor.LookupPropertyEditorMemberViewItems.ShouldContain(modelMemberViewItems.First(item => item.Id==nameof(Order.Product)));
            orderPropertyEditor.LookupPropertyEditorMemberViewItems.Count().ShouldBe(1);
        }
        
        [XpandTest][Order(1)]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        public void Cascade_ColumnFilter_Lists_the_CascadeMemberViewItem_LookupListView_Visible_Members_of_the_same_type_to_current_memberViewItem_key_type(string viewId){
            var application = ClientLookupCascadeModule().Application;
            var applicationModel = application.Model;
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            var productModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Product));
            productModelMemberViewItem.PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
            var accesoryModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Accessory));
            accesoryModelMemberViewItem.PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
                
            var productPropertyEditor = ((IModelMemberViewItemLookupCascadePropertyEditor) productModelMemberViewItem).LookupCascade;
            productPropertyEditor.CascadeMemberViewItem=modelMemberViewItems.First(item => item.Id==nameof(Order.Accessory));
            var accesoryLIstView = (IModelListView)applicationModel.FindModelView(application.FindListViewId(typeof(Accessory)));
            accesoryLIstView.Columns[nameof(Accessory.Product)].Remove();
            var modelColumn = accesoryLIstView.Columns.AddNode<IModelColumn>("Product");
            modelColumn.PropertyName=$"{nameof(Product)}.Oid";
            productPropertyEditor.CascadeMemberViewItem.View = accesoryLIstView;
            productPropertyEditor.CascadeColumnFilters.Count().ShouldBe(1);
            productPropertyEditor.CascadeColumnFilters.ShouldContain(modelColumn);
        }
        [XpandTest]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        [Order(2)]
        public void DatasourceModel_LookupListView_Lookup_Lists_Views_With_Members_Using_the_cascade_Editor(string viewId){
            var application = ClientLookupCascadeModule().Application;
            var applicationModel = application.Model;
        
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Product)).PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
            modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Accessory)).PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
        
            var clientDatasourceLookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
            clientDatasourceLookupView.LookupListViews.ShouldContain(applicationModel.FindLookupListView(typeof(Product)));
            clientDatasourceLookupView.LookupListViews.ShouldContain(applicationModel.FindLookupListView(typeof(Accessory)));
            
        }

        [XpandTest]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")][Order(3)]
        public void PropertyEditorModel_Is_Visible_Only_for_members_that_use_the_editor(string viewId){
            var application = ClientLookupCascadeModule().Application;
            var applicationModel = application.Model;
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            var productModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Product));
            productModelMemberViewItem.PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
            var accesoryModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Accessory));
            accesoryModelMemberViewItem.PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
        
            productModelMemberViewItem.IsPropertyVisible(nameof(IModelMemberViewItemLookupCascadePropertyEditor.LookupCascade)).ShouldBe(true);
            accesoryModelMemberViewItem.IsPropertyVisible(nameof(IModelMemberViewItemLookupCascadePropertyEditor.LookupCascade)).ShouldBe(true);
        }

    }
}