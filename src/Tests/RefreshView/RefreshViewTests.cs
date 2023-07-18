using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.RefreshView.Tests.BOModel;


namespace Xpand.XAF.Modules.RefreshView.Tests{
    [NonParallelizable]
    public class RefreshViewTests : BaseTest{
        

        [Test]
        [XpandTest]
        [Apartment(ApartmentState.STA)]
        public async Task Refresh_ListView_When_Root(){
            using var application = RefreshViewModule(nameof(Refresh_ListView_When_Root)).Application;
            var items = application.Model.ToReactiveModule<IModelReactiveModuleRefreshView>().RefreshView.Items;
            
            var item = items.AddNode<IModelRefreshViewItem>();
            item.View = application.Model.BOModel.GetClass(typeof(RV)).DefaultListView;
            item.Interval = TimeSpan.FromMilliseconds(500);
            
            application.Logon();
            var listView = application.NewObjectView<ListView>(typeof(RV));
            var reloaded = listView.CollectionSource.WhenCollectionReloaded().Select(tuple => tuple).FirstAsync().SubscribeReplay();
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(listView);
            
            var objectSpace = application.CreateObjectSpace();
            objectSpace.CommitChanges();

            await reloaded.Timeout(Timeout);
            application.CreateViewWindow().SetView(listView);
            
            objectSpace = application.CreateObjectSpace();
            var guid = objectSpace.CreateObject<RV>().Oid;
            objectSpace.CommitChanges();

            var o = ((IEnumerable) listView.CollectionSource.Collection).Cast<RV>().FirstOrDefault(rv => rv.Oid==guid);
            o.ShouldNotBeNull();
        }



        private static RefreshViewModule RefreshViewModule(string title,
            Platform platform = Platform.Win){
            var application = platform.NewApplication<RefreshViewModule>();
            return application.AddModule<RefreshViewModule>(title,typeof(RV));
        }
        
    }
}