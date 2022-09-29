using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ViewItemValue.Tests{
    public class ModelLogicTests:ViewItemValueCommonTest{
        [Test][XpandTest()]
        public void ModelObjectView_datasource_contains_only_views_with_Lookup_Members() {
            using var application=ViewItemValueModule().Application;
            var viewItemValueItem = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>()
                .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
            viewItemValueItem.ObjectViews.Count().ShouldBeGreaterThanOrEqualTo(1);
            viewItemValueItem.ObjectViews.Select(view => view.ModelClass.TypeInfo.Type).ShouldContain(typeof(Accessory));
        }
        [Test][XpandTest()]
        public void MemberViewItems_datasource_contains_only_Lookup_Members() {
            using var application=ViewItemValueModule().Application;
            var viewItemValueItem = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>()
                .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
            viewItemValueItem.ObjectView = application.FindModelDetailView(typeof(Order));

            var item = viewItemValueItem.Members.AddNode<IModelViewItemValueObjectViewItem>();
            item.MemberViewItems.Count().ShouldBe(5);
            item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.Accessory)).ShouldNotBeNull();
            item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.Product)).ShouldNotBeNull();
            item.MemberViewItems.FirstOrDefault(viewItem => viewItem.Id==nameof(Order.OrderID)).ShouldNotBeNull();
        }
    }
}