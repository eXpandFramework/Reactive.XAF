using System;
using System.Linq;
using System.Reactive.Linq;
using System.Web;
using DevExpress.ExpressApp.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Bytes;
using Xpand.Extensions.String;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupCascade.Tests{
    public class DataSourceTests:LookupCascadeBaseTest{
        [XpandTest]
        [Test]
        [Order(1)]
        public void First_record_is_the_listview_columns_captions(){
            using (var application = ClientLookupCascadeModule().Application){
                var lookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
                var producLookupListView = application.FindModelClass(typeof(Product)).DefaultListView;
                lookupView.LookupListView = producLookupListView;

                var clientDataSource = application.CreateClientDataSource().ToArray();
                
                var bytes = Convert.FromBase64String(clientDataSource.First(_ => _.viewId==producLookupListView.Id).objects);
                var products = JsonConvert.DeserializeObject<dynamic>(bytes.Unzip());
                ((int) products.Count).ShouldBe(2);
                $"{products[0].Key}".ShouldBe(LookupCascadeService.FieldNames);
                $"{products[0].Columns}".ShouldBe(string.Join("&", HttpUtility.UrlEncode(producLookupListView.Columns[nameof(Product.ProductName)].Caption),
                    HttpUtility.UrlEncode(producLookupListView.Columns[nameof(Product.Price)].Caption)));
            }
        }
        [XpandTest]
        [Test][Order(2)]
        public void Second_record_is_the_NA_record(){
            using (var application = ClientLookupCascadeModule().Application){
                var lookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
                var producLookupListView = application.FindModelClass(typeof(Product)).DefaultListView;
                lookupView.LookupListView = producLookupListView;
        
                var clientDataSource = application.CreateClientDataSource().ToArray();
                
                var bytes = Convert.FromBase64String(clientDataSource.First(_ => _.viewId==producLookupListView.Id).objects);
                var products = JsonConvert.DeserializeObject<dynamic>(bytes.Unzip());
                ((int) products.Count).ShouldBe(2);
                $"{products[1].Key}".ShouldBe(string.Empty);
                HttpUtility.UrlDecode($"{products[1].Columns}").ShouldBe(LookupCascadeService.NA.Repeat(2,"&"));
            }
        }
        
        [XpandTest]
        [Test]
        public void Third_record_is_the_values_of_all_visible_columns(){
            using (var application = ClientLookupCascadeModule().Application){
                var lookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
                var accesporyLookupListView = application.FindModelClass(typeof(Accessory)).DefaultListView;
                accesporyLookupListView.Columns[nameof(Accessory.Product)].Remove();
                accesporyLookupListView.Columns[nameof(Accessory.IsGlobal)].Index=-1;
                var productColumn = accesporyLookupListView.Columns.AddNode<IModelColumn>("Product");
                productColumn.Index = 2;
                productColumn.PropertyName = "Product.Oid";
                lookupView.LookupListView = accesporyLookupListView;
                var objectSpace = application.CreateObjectSpace();
                var accessory = objectSpace.CreateObject<Accessory>();
                accessory.AccessoryName = "acc";
                accessory.Product=objectSpace.CreateObject<Product>();
                objectSpace.CommitChanges();
                
                var clientDataSource = application.CreateClientDataSource().ToArray();
                
                var bytes = Convert.FromBase64String(clientDataSource.First(_ => _.viewId==accesporyLookupListView.Id).objects);
                var products = JsonConvert.DeserializeObject<dynamic>(bytes.Unzip());
                ((int) products.Count).ShouldBe(3);
                objectSpace.GetObjectByHandle($"{products[2].Key}").ShouldBe(accessory);
                HttpUtility.UrlDecode($"{products[2].Columns}").ShouldBe($"{accessory.AccessoryName}&{accessory.Product.Oid}");
            }
        }
        
        [XpandTest]
        [Test]
        public void Each_Lookupview_has_a_different_datasource(){
            using (var application = ClientLookupCascadeModule().Application){
                var lookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
                var productLookupListView = application.FindModelClass(typeof(Product)).DefaultListView;
                lookupView.LookupListView = productLookupListView;
                lookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
                var accesoryListView = application.FindModelClass(typeof(Accessory)).DefaultListView;
                lookupView.LookupListView = accesoryListView;
                var objectSpace = application.CreateObjectSpace();
                var product = objectSpace.CreateObject<Product>();
                var accessory = objectSpace.CreateObject<Accessory>();
                objectSpace.CommitChanges();
                
                var clientDataSource = application.CreateClientDataSource().ToArray();
                
                clientDataSource.Length.ShouldBe(2);
                var bytes = Convert.FromBase64String(clientDataSource.First(_ => _.viewId==productLookupListView.Id).objects);
                var objects = JsonConvert.DeserializeObject<dynamic>(bytes.Unzip());
                ((int) objects.Count).ShouldBe(3);
                objectSpace.GetObjectByHandle($"{objects[2].Key}").ShouldBe(product);
                
                bytes = Convert.FromBase64String(clientDataSource.First(_ => _.viewId==accesoryListView.Id).objects);
                objects = JsonConvert.DeserializeObject<dynamic>(bytes.Unzip());
                ((int) objects.Count).ShouldBe(3);
                objectSpace.GetObjectByHandle($"{objects[2].Key}").ShouldBe(accessory);
            }
        }

    }
}