using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Model;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;

namespace Xpand.XAF.Modules.LookupCascade.Tests.ModelLogic{
    public class PropertyEditorModel:LookupCascadeBaseTest{
        [XpandTest]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        public void Is_Visible_Only_for_members_that_use_the_editor(string viewId){
            var application = ClientLookupCascadeModule(nameof(Is_Visible_Only_for_members_that_use_the_editor)).Application;
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