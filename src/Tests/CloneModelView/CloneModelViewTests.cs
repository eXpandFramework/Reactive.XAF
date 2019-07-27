using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.CloneModelView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Xpand.XAF.Modules.CloneModelView.Tests{
    public static class MyRxExtensions{
        

        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source,int retryCount = 3,
            Func<int, TimeSpan> strategy = null,Func<Exception, bool> retryOnError = null,IScheduler scheduler = null){
            strategy = strategy ?? (n =>TimeSpan.FromSeconds(Math.Pow(n, 2))) ;
            var attempt = 0;
            retryOnError = retryOnError ?? (_ => true);
            return Observable.Defer(() => (++attempt == 1 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => (true, item, (Exception)null))
                    .Catch<(bool, T, Exception), Exception>(e =>retryOnError(e)? Observable.Throw<(bool, T, Exception)>(e)
                            : Observable.Return<(bool, T, Exception)>((false, default, e))))
                .Retry(retryCount)
                .SelectMany(t => t.Item1
                    ? Observable.Return(t.Item2)
                    : Observable.Throw<T>(t.Item3));
        }

        public static IObservable<T> DelaySubscription<T>(this IObservable<T> source,
            TimeSpan delay, IScheduler scheduler = null){
            if (scheduler == null) return Observable.Timer(delay).SelectMany(_ => source);
            return Observable.Timer(delay, scheduler).SelectMany(_ => source);
        }
    }

    [Collection(nameof(CloneModelViewModule))]
    public class CloneModelViewTests : BaseTest{
        protected async Task Execute(Action action){
            await Observable.Defer(() => Observable.Start(action)).RetryWithBackoff(3,retryOnError:exception => true).FirstAsync();
        }

        [Theory]
        [InlineData(CloneViewType.LookupListView, Platform.Win)]
        [InlineData(CloneViewType.ListView,Platform.Win)]
        [InlineData(CloneViewType.DetailView,Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        [InlineData(CloneViewType.DetailView,Platform.Web)]
        internal async Task Clone_Model_View(CloneViewType cloneViewType, Platform platform){

            await Execute(() => {
                var cloneViewId = $"{nameof(Clone_Model_View)}{platform}_{cloneViewType}";

                var application = DefaultCloneModelViewModule(info => {
                    var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId);
                    info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
                }, platform).Application;
                var modelView = application.Model.Views[cloneViewId];
                modelView.ShouldNotBeNull();
                modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
                modelView.Id.ShouldBe(cloneViewId);
                application.Dispose();
            });

        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Clone_multiple_Model_Views(Platform platform){
            await Execute(() => {
                var cloneViewId = $"{nameof(Clone_multiple_Model_Views)}{platform}_";
                var cloneViewTypes = Enum.GetValues(typeof(CloneViewType)).Cast<CloneViewType>();
                var application = DefaultCloneModelViewModule(info => {
                    foreach (var cloneViewType in cloneViewTypes){
                        var cloneModelViewAttribute =
                            new CloneModelViewAttribute(cloneViewType, $"{cloneViewId}{cloneViewType}");
                        info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
                    }
                }, platform).Application;
                foreach (var cloneViewType in cloneViewTypes){
                    var viewId = $"{cloneViewId}{cloneViewType}";
                    var modelView = application.Model.Views[viewId];
                    modelView.ShouldNotBeNull();
                    modelView.GetType().Name.ShouldBe($"Model{cloneViewType.ToString().Replace("Lookup", "")}");
                    modelView.Id.ShouldBe(viewId);
                }

                application.Dispose();
            });
        }

        [Theory]
        [InlineData(CloneViewType.LookupListView, Platform.Win)]
        [InlineData(CloneViewType.ListView, Platform.Win)]
        [InlineData(CloneViewType.DetailView, Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        [InlineData(CloneViewType.DetailView,Platform.Web)]
        internal async Task Clone_Model_View_and_make_it_default(CloneViewType cloneViewType, Platform platform){
            await Execute(() => {
                var cloneViewId = $"{nameof(Clone_Model_View_and_make_it_default)}_{cloneViewType}{platform}";

                var application = DefaultCloneModelViewModule(info => {
                    var cloneModelViewAttribute = new CloneModelViewAttribute(cloneViewType, cloneViewId, true);
                    info.FindTypeInfo(typeof(CMV)).AddAttribute(cloneModelViewAttribute);
                }, platform).Application;
                var modelView = application.Model.Views[cloneViewId].AsObjectView;

                ((IModelView) modelView.ModelClass.GetPropertyValue($"Default{cloneViewType}")).Id
                    .ShouldBe(cloneViewId);
                application.Dispose();
            });
        }

        [Theory]
        [InlineData(CloneViewType.LookupListView, Platform.Win)]
        [InlineData(CloneViewType.ListView, Platform.Win)]
        [InlineData(CloneViewType.LookupListView,Platform.Web)]
        [InlineData(CloneViewType.ListView,Platform.Web)]
        internal async Task Clone_Model_ListView_and_change_its_detailview(CloneViewType cloneViewType, Platform platform){
            await Execute(() => {
                var cloneViewId = $"{nameof(Clone_Model_ListView_and_change_its_detailview)}{platform}_";
                var listViewId = $"{cloneViewId}{cloneViewType}";
                var detailViewId = $"{cloneViewType}DetailView";
                var application = DefaultCloneModelViewModule(info => {
                    var typeInfo = info.FindTypeInfo(typeof(CMV));
                    typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, detailViewId));
                    typeInfo.AddAttribute(new CloneModelViewAttribute(cloneViewType, listViewId)
                        {DetailView = detailViewId});
                }, platform).Application;
                var modelListView = (IModelListView) application.Model.Views[listViewId];
                modelListView.DetailView.Id.ShouldBe(detailViewId);
                application.Dispose();
            });
        }

        private static CloneModelViewModule DefaultCloneModelViewModule(Action<ITypesInfo> customizeTypesInfo,
            Platform platform){
            var application = platform.NewApplication();
            application.WhenCustomizingTypesInfo().FirstAsync(info => {
                    customizeTypesInfo(info);
                    return true;
                })
                .Subscribe();
            var cloneModelViewModule = new CloneModelViewModule();
            cloneModelViewModule.AdditionalExportedTypes.AddRange(new[]{typeof(CMV)});
            cloneModelViewModule.RequiredModuleTypes.Add(typeof(ReactiveModule));
            application.SetupDefaults(cloneModelViewModule);
            return cloneModelViewModule;
        }
    }
}