using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib.Common {
    public static class CommonTestScenario {
        public static async Task<Frame> TestListViewProcessSelectedItem(this XafApplication application,Type objectType) {
            var window = await TestListView(application, objectType);
            var action = window.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction;
            var frameViewChanged = application.WhenFrameViewChanged().Select(frame1 => frame1)
                .WhenFrame(ViewType.DetailView).FirstAsync().SubscribeReplay();

            action.DoExecute(objectSpace => {
                var objects = window.View.AsListView().CollectionSource.Objects().Take(1).ToArray();
                return objects.Select(objectSpace.GetObject).ToArray();
            },true);

            var frame = await frameViewChanged;
            
            frame.View.ShouldBeOfType<DetailView>();
            return frame;
        }

        public static async Task<Window> TestListView(this XafApplication application, Type objectType) {
            if (application.GetPlatform() == Platform.Blazor) {
                application.CallMethod("CreateMainWindow");
            }
            var listView = application.NewListView(objectType);
            var whenProxyCollectionChanged = ((ProxyCollection) listView.CollectionSource.Collection).Count.ReturnObservable()
                .SelectMany(_ => listView.CollectionSource.WhenProxyCollectionChanged()).SubscribeReplay();
            var window = application.CreateViewWindow(() => listView);
            await whenProxyCollectionChanged.FirstAsync();
            return window;
        }
    }
}