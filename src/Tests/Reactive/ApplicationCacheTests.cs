using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.Data.Filtering;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Filter;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests {
    public class ApplicationCacheTests:ReactiveCommonTest {

        [Test][Order(0)]
        public async Task Contains_Existing_Items() {
            using (var application = DefaultReactiveModule().Application) {
                using var objectSpace = application.CreateObjectSpace();
                objectSpace.CreateObject<R>().CommitChanges();
                var cache = new ConcurrentDictionary<long,R>();
            
                using var testObserver = application.Cache(cache).Test();
            
                testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
                cache.Keys.Count.ShouldBe(1);

                await application.UseObjectSpace(space => space.Delete(space.GetObjects<R>()));
            }
        }
        
        [Test][Order(1)]
        public async Task Contains_New_Items() {
            using (var application = DefaultReactiveModule().Application) {
                var cache = new ConcurrentDictionary<long,R>();
                using var testObserver = application.Cache(cache).WhenNotEmpty().Take(1).Test();
            
                using var objectSpace = application.CreateObjectSpace();
                var o = objectSpace.CreateObject<R>();
                o.CommitChanges();
                testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);

                cache.ContainsKey(o.Oid).ShouldBeTrue();
            
                await application.UseObjectSpace(space => space.Delete(space.GetObjects<R>()));
            }
        }

        [Test][Order(2)]
        public void Contains_Updated_Items() {
            using var application = DefaultReactiveModule().Application;
            using var objectSpace = application.CreateObjectSpace();
            var o = objectSpace.CreateObject<R>();
            o.CommitChanges();
            var cache = new ConcurrentDictionary<long,R>();
            using var testObserver = application.Cache(cache).Test();

            var space = application.CreateObjectSpace();
            o = space.GetObject(o);
            o.Test = nameof(Contains_Updated_Items);
            o.CommitChanges();
            
            
            cache[o.Oid].Test.ShouldBe(nameof(Contains_Updated_Items));
        }
        [Test][Order(2)]
        public void Not_Contains_Updated_Items() {
            using var application = DefaultReactiveModule().Application;
            using var objectSpace = application.CreateObjectSpace();
            var o = objectSpace.CreateObject<R>();
            o.CommitChanges();
            var cache = new ConcurrentDictionary<long,R>();
            using var testObserver = application.Cache(cache,CriteriaOperator.FromLambda<R>(r => r.Test==null)).Test();

            var space = application.CreateObjectSpace();
            o = space.GetObject(o);
            o.Test = nameof(Contains_Updated_Items);
            o.CommitChanges();
            
            
            cache.ContainsKey(o.Oid).ShouldBeFalse();
        }

        [Test][Order(3)]
        public async Task Not_Contains_Deleted_Items() {
            using var application = DefaultReactiveModule().Application;
            using var objectSpace = application.CreateObjectSpace();
            var o = objectSpace.CreateObject<R>();
            o.CommitChanges();
            var cache = new ConcurrentDictionary<long,R>();
            using var testObserver = application.Cache(cache).Test();
            cache.ContainsKey(o.Oid).ShouldBeTrue();
            
            var space = application.CreateObjectSpace();
            space.Delete(space.GetObject(o));
            space.CommitChanges();
            
            
            cache.ContainsKey(o.Oid).ShouldBeFalse();
            
            await application.UseObjectSpace(space1 => space1.Delete(space1.GetObjects<R>()));
        }

    }
}