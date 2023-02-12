using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests {
    public class ApplicationCacheTests:ReactiveCommonAppTest {

        [Test][Order(0)]
        public async Task Contains_Existing_Items() {
            using var objectSpace = Application.CreateObjectSpace();
            objectSpace.CreateObject<R>().CommitChanges();
            var cache = new ConcurrentDictionary<long,R>();
            
            using var testObserver = Application.Cache(cache).Test();
            
            testObserver.AwaitDone(Timeout).ItemCount.ShouldBe(1);
            cache.Keys.Count.ShouldBe(1);

            await Application.UseObjectSpace(space => space.Delete(space.GetObjects<R>()));
        }
        
        [Test][Order(1)]
        public void Contains_New_Items() {
            var cache = new ConcurrentDictionary<long,R>();
            using var testObserver = Application.Cache(cache).Test();
            
            using var objectSpace = Application.CreateObjectSpace();
            var o = objectSpace.CreateObject<R>();
            o.CommitChanges();

            cache.ContainsKey(o.Oid).ShouldBeTrue();
        }

        [Test][Order(2)]
        public void Contains_Updated_Items() {
            using var objectSpace = Application.CreateObjectSpace();
            var o = objectSpace.CreateObject<R>();
            o.CommitChanges();
            var cache = new ConcurrentDictionary<long,R>();
            using var testObserver = Application.Cache(cache).Test();

            var space = Application.CreateObjectSpace();
            o = space.GetObject(o);
            o.Test = nameof(Contains_Updated_Items);
            o.CommitChanges();
            
            
            cache[o.Oid].Test.ShouldBe(nameof(Contains_Updated_Items));
        }

        [Test][Order(3)]
        public void Not_Contains_Deleted_Items() {
            using var objectSpace = Application.CreateObjectSpace();
            var o = objectSpace.CreateObject<R>();
            o.CommitChanges();
            var cache = new ConcurrentDictionary<long,R>();
            using var testObserver = Application.Cache(cache).Test();

            var space = Application.CreateObjectSpace();
            space.Delete(space.GetObject(o));
            space.CommitChanges();
            
            
            cache.ContainsKey(o.Oid).ShouldBeFalse();
        }

    }
}