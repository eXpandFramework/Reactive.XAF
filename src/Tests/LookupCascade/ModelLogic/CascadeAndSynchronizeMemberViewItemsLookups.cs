using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Model;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;

namespace Xpand.XAF.Modules.LookupCascade.Tests.ModelLogic{
    public class CascadeAndSynchronizeMemberViewItemsLookups:LookupCascadeBaseTest{
        [XpandTest]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        public void Contain_members_that_use_the_Editor(string viewId){
            var application = ClientLookupCascadeModule(nameof(Contain_members_that_use_the_Editor)).Application;
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
    }
}