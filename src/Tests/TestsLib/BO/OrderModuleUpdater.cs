using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Xpand.TestsLib.BO{
    public class OrderModuleUpdater:ModuleUpdater{
        public OrderModuleUpdater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion){
        }

        public override void UpdateDatabaseAfterUpdateSchema(){
            base.UpdateDatabaseAfterUpdateSchema();

            if (!ObjectSpace.GetObjectsQuery<Order>().Any()){
                for (int i = 0; i < 2; i++){
                    var product = ObjectSpace.CreateObject<Product>();
                    product.ProductName = $"{nameof(Product.ProductName)}{i}";
                    var accessory = ObjectSpace.CreateObject<Accessory>();
                    accessory.Product=product;
                    accessory.AccessoryName = $"{nameof(Accessory.AccessoryName)}{i}";
                    var order = ObjectSpace.CreateObject<Order>();
                    order.Product=product;
                    order.Accessory=accessory;
                    order.AggregatedOrders.Add(order);
                }
                ObjectSpace.CommitChanges();
            }
        }
    }
}