using System.Linq;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Model;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;

namespace Xpand.XAF.Modules.LookupCascade.Tests.ModelLogic{
    public class CascadeColumnFilter:LookupCascadeBaseTest{
        [XpandTest]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        public void Lists_the_CascadeMemberViewItem_LookupListView_Visible_Members_of_the_same_type_to_current_memberViewItem_key_type(string viewId){
            var application = ClientLookupCascadeModule(nameof(Lists_the_CascadeMemberViewItem_LookupListView_Visible_Members_of_the_same_type_to_current_memberViewItem_key_type)).Application;
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
                
    }
}