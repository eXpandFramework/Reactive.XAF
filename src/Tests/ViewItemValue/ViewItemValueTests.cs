using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ViewItemValue.Tests{
    public class ViewItemValueTests:ViewItemValueCommonTest{
        [Test][XpandTest()]
        public void ViewItemValue_action_is_inactive_by_default(){
            using var application=ViewItemValueModule().Application;
            var compositeView = application.NewView(application.FindModelDetailView(typeof(Order)));
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(compositeView);

            viewWindow.Action<ViewItemValueModule>().ViewItemValue().Active["EmptyItems"].ShouldBeFalse();
        }
        [Test][XpandTest()]
        public void ViewItemValue_action_is_active_When_ModelViewItemValueItem(){
            using var application=ViewItemValueModule().Application;
            var viewItemValueItem = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>()
                .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
            viewItemValueItem.ObjectView = application.FindModelDetailView(typeof(Order));
            var item = viewItemValueItem.Members.AddNode<IModelViewItemValueObjectViewItem>();
            item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Product));
            item = viewItemValueItem.Members.AddNode<IModelViewItemValueObjectViewItem>();
            item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Accessory));
            var compositeView = application.NewView(viewItemValueItem.ObjectView);
            var viewWindow = application.CreateViewWindow();
                
            viewWindow.SetView(compositeView);

            var action = viewWindow.Action<ViewItemValueModule>().ViewItemValue();
            action.Active.ResultValue.ShouldBeTrue();
            action.Items.Count.ShouldBe(2);
        }
        [Test][XpandTest()]
        public void ViewItemValue_action_saves_view_lookupobject_keyvalue(){
            using var application=ViewItemValueModule().Application;
            var viewItemValueItem = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>()
                .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
            viewItemValueItem.ObjectView = application.FindModelDetailView(typeof(Order));
            var item = viewItemValueItem.Members.AddNode<IModelViewItemValueObjectViewItem>();
            item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Product));
            var compositeView = application.NewView(viewItemValueItem.ObjectView);
            application.CreateObjectSpace().CreateObject<Product>().ObjectSpace.CommitChanges();
            var order = compositeView.ObjectSpace.CreateObject<Order>();
            compositeView.CurrentObject = order;
            order.Product = compositeView.ObjectSpace.GetObjects<Product>().First();
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(compositeView);

            var action = viewWindow.Action<ViewItemValueModule>().ViewItemValue();
            action.DoExecute(space => new[]{compositeView.CurrentObject});

            var objectSpace = application.CreateObjectSpace();
            var viewItemValue = objectSpace.GetObjectsQuery<BusinessObjects.ViewItemValueObject>().FirstOrDefault();
            viewItemValue.ShouldNotBeNull();
            viewItemValue.ObjectView.ShouldBe(viewItemValueItem.ObjectView.Id);
            viewItemValue.ViewItemId.ShouldBe(nameof(Order.Product));
            viewItemValue.ViewItemValue.ShouldBe(objectSpace.GetKeyValue(order.Product).ToString());
        }

        [Test][XpandTest()]
        public void When_DetailView_with_new_object_assign_defaultLookupObjects(){
            using var application=ViewItemValueModule().Application;
            var viewItemValueItem = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>()
                .ViewItemValue.Items.AddNode<IModelViewItemValueItem>();
            viewItemValueItem.ObjectView = application.FindModelDetailView(typeof(Order));
            var item = viewItemValueItem.Members.AddNode<IModelViewItemValueObjectViewItem>();
            item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Product));
            var objectSpace = application.CreateObjectSpace();
            var product = objectSpace.CreateObject<Product>();
            objectSpace.CommitChanges();
            var viewItemValue = objectSpace.CreateObject<BusinessObjects.ViewItemValueObject>();
            viewItemValue.ViewItemValue = product.Oid.ToString();
            viewItemValue.ViewItemId = nameof(Order.Product);
            viewItemValue.ObjectView = viewItemValueItem.ObjectViewId;
            objectSpace.CommitChanges();
            var order = objectSpace.CreateObject<Order>();

            application.CreateViewWindow().SetView(application.CreateDetailView(objectSpace, order));

            order.Product.ShouldNotBeNull();
            order.Product.Oid.ShouldBe(product.Oid);
        }

    }
}