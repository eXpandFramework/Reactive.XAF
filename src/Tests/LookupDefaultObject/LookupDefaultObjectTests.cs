using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.TestsLib.BO;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupDefaultObject.Tests{
    public class LookupDefaultObjectTests:LookupDefaultObjectBaseTest{
        [Test][XpandTest()]
        public void LookupDefaultObject_action_is_inactive_bydefault(){
            using (var application=LookupDefaultObjectModule().Application){
                var compositeView = application.NewView(application.FindModelDetailView(typeof(Order)));
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);

                viewWindow.Action<LookupDefaultObjectModule>().LookupDefaultObject().Active["EmptyItems"].ShouldBeFalse();
            }

        }
        [Test][XpandTest()]
        public void LookupDefaultObject_action_is_active_When_ModelLookupDefaultObjectItem(){
            using (var application=LookupDefaultObjectModule().Application){
                var lookupDefaultObjectItem = application.Model.ToReactiveModule<IModelReactiveModulesLookupDefaultObject>()
                    .LookupDefaultObject.Items.AddNode<IModelLookupDefaultObjectItem>();
                lookupDefaultObjectItem.ObjectView = application.FindModelDetailView(typeof(Order));
                var item = lookupDefaultObjectItem.Members.AddNode<IModelLookupDefaultObjectObjectViewItem>();
                item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Product));
                item = lookupDefaultObjectItem.Members.AddNode<IModelLookupDefaultObjectObjectViewItem>();
                item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Accessory));
                var compositeView = application.NewView(lookupDefaultObjectItem.ObjectView);
                var viewWindow = application.CreateViewWindow();
                
                viewWindow.SetView(compositeView);

                var action = viewWindow.Action<LookupDefaultObjectModule>().LookupDefaultObject();
                action.Active.ResultValue.ShouldBeTrue();
                action.Items.Count().ShouldBe(2);
            }
        }
        [Test][XpandTest()]
        public void LookupDefaultObject_action_saves_view_lookupobject_keyvalue(){
            using (var application=LookupDefaultObjectModule().Application){
                var lookupDefaultObjectItem = application.Model.ToReactiveModule<IModelReactiveModulesLookupDefaultObject>()
                    .LookupDefaultObject.Items.AddNode<IModelLookupDefaultObjectItem>();
                lookupDefaultObjectItem.ObjectView = application.FindModelDetailView(typeof(Order));
                var item = lookupDefaultObjectItem.Members.AddNode<IModelLookupDefaultObjectObjectViewItem>();
                item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Product));
                var compositeView = application.NewView(lookupDefaultObjectItem.ObjectView);
                application.CreateObjectSpace().CreateObject<Product>().ObjectSpace.CommitChanges();
                var order = compositeView.ObjectSpace.CreateObject<Order>();
                compositeView.CurrentObject = order;
                order.Product = compositeView.ObjectSpace.GetObjects<Product>().First();
                var viewWindow = application.CreateViewWindow();
                viewWindow.SetView(compositeView);

                var action = viewWindow.Action<LookupDefaultObjectModule>().LookupDefaultObject();
                action.DoExecute(space => new[]{compositeView.CurrentObject});

                var objectSpace = application.CreateObjectSpace();
                var lookupDefaultObject = objectSpace.GetObjectsQuery<BusinessObjects.LookupDefaultObject>().FirstOrDefault();
                lookupDefaultObject.ShouldNotBeNull();
                lookupDefaultObject.ObjectView.ShouldBe(lookupDefaultObjectItem.ObjectView.Id);
                lookupDefaultObject.MemberName.ShouldBe(nameof(Order.Product));
                lookupDefaultObject.KeyValue.ShouldBe(objectSpace.GetKeyValue(order.Product).ToString());
            }
        }

        [Test][XpandTest()]
        public void When_DetailView_with_new_object_assign_defaultLookupObjects(){
            using (var application=LookupDefaultObjectModule().Application){
                var lookupDefaultObjectItem = application.Model.ToReactiveModule<IModelReactiveModulesLookupDefaultObject>()
                    .LookupDefaultObject.Items.AddNode<IModelLookupDefaultObjectItem>();
                lookupDefaultObjectItem.ObjectView = application.FindModelDetailView(typeof(Order));
                var item = lookupDefaultObjectItem.Members.AddNode<IModelLookupDefaultObjectObjectViewItem>();
                item.MemberViewItem = item.MemberViewItems.First(viewItem => viewItem.Id == nameof(Order.Product));
                var objectSpace = application.CreateObjectSpace();
                var product = objectSpace.CreateObject<Product>();
                objectSpace.CommitChanges();
                var lookupDefaultObject = objectSpace.CreateObject<BusinessObjects.LookupDefaultObject>();
                lookupDefaultObject.KeyValue = product.Oid.ToString();
                lookupDefaultObject.MemberName = nameof(Order.Product);
                lookupDefaultObject.ObjectView = lookupDefaultObjectItem.ObjectViewId;
                objectSpace.CommitChanges();
                var order = objectSpace.CreateObject<Order>();

                application.CreateDetailView(objectSpace, order);

                order.Product.ShouldNotBeNull();
                order.Product.Oid.ShouldBe(product.Oid);
            }
        }

    }
}