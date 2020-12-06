using Xpand.TestsLib;

namespace Xpand.XAF.Modules.ViewWizard.Tests{
    public class ModelLogicTests:ViewWizardCommonTest{
        

        // [Test][XpandTest()]
        // public void ModelObjectView_datasource_contains_only_views_with_Lookup_Members(){
        //     using (var applicatin=ViewWizardModule().Application){
        //         var viewItemValueItem = applicatin.Model.ToReactiveModule<IModelReactiveModulesViewWizard>()
        //             .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
        //         viewItemValueItem.ObjectViews.Count().ShouldBeGreaterThanOrEqualTo(1);
        //         viewItemValueItem.ObjectViews.Select(view => view.ModelClass.TypeInfo.Type).ShouldContain(typeof(Accessory));
        //     }
        // }
        // [Test][XpandTest()]
        // public void MemberViewItems_datasource_contains_only_Lookup_Members(){
        //     using (var applicatin=ViewWizardModule().Application){
        //         var viewItemValueItem = applicatin.Model.ToReactiveModule<IModelReactiveModulesViewWizard>()
        //             .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
        //         viewItemValueItem.ObjectView = applicatin.FindModelDetailView(typeof(Order));
        //
        //         var item = viewItemValueItem.Members.AddNode<IModelViewItemValueObjectViewItem>();
        //         item.MemberViewItems.Count().ShouldBe(2);
        //         item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.Accessory)).ShouldNotBeNull();
        //         item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.Product)).ShouldNotBeNull();
        //     }
        // }
    }
}