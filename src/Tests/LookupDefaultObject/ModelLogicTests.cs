using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Attributes;
using Xpand.TestsLib.BO;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupDefaultObject.Tests{
    public class ModelLogicTests:LookupDefaultObjectBaseTest{
        [Test][XpandTest()]
        public void ModelObjectView_datasource_contains_only_views_with_Lookup_Members(){
            using (var applicatin=LookupDefaultObjectModule().Application){
                var lookupDefaultObjectItem = applicatin.Model.ToReactiveModule<IModelReactiveModulesLookupDefaultObject>()
                    .LookupDefaultObject.Items.AddNode<IModelLookupDefaultObjectItem>();
                lookupDefaultObjectItem.ObjectViews.Count().ShouldBeGreaterThanOrEqualTo(1);
                lookupDefaultObjectItem.ObjectViews.Select(view => view.ModelClass.TypeInfo.Type).ShouldContain(typeof(Accessory));
            }
        }
        [Test][XpandTest()]
        public void MemberViewItems_datasource_contains_only_Lookup_Members(){
            using (var applicatin=LookupDefaultObjectModule().Application){
                var lookupDefaultObjectItem = applicatin.Model.ToReactiveModule<IModelReactiveModulesLookupDefaultObject>()
                    .LookupDefaultObject.Items.AddNode<IModelLookupDefaultObjectItem>();
                lookupDefaultObjectItem.ObjectView = applicatin.FindModelDetailView(typeof(Order));

                var item = lookupDefaultObjectItem.Members.AddNode<IModelLookupDefaultObjectObjectViewItem>();
                item.MemberViewItems.Count().ShouldBe(2);
                item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.Accessory)).ShouldNotBeNull();
                item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.Product)).ShouldNotBeNull();
            }
        }
    }
}