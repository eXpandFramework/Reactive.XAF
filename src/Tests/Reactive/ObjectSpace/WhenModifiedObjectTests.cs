using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpace {
    public class WhenModifiedObjectTests : ReactiveCommonAppTest {
        private R _r;
        private TestObserver<R> _testObserver;

        [Test][Order(0)]
        public void Emit_once_when_many_properties_change() {
            var objectSpace = Application.CreateObjectSpace();
            _r = objectSpace.CreateObject<R>();

            _testObserver = objectSpace.WhenModifiedObjects<R>().Test();

            _r.Test = nameof(R.Test);
            _r.Test1 = nameof(R.Test1);
            
            _testObserver.ItemCount.ShouldBe(1);
            
            
        }
        
        [Test][Order(10)]
        public void Emit_again_for_same_properties_After_commit() {
            _r.ObjectSpace.CommitChanges();

            _r.Test = null;
            _r.Test1 = null;
            
            _testObserver.ItemCount.ShouldBe(2);
            
            
        }

        [Test][Order(20)]
        public void Emit_once_when_selected_properties_change() {
            var objectSpace = Application.CreateObjectSpace();
            _r = objectSpace.CreateObject<R>();

            _testObserver = objectSpace.WhenModifiedObjects<R>(nameof(R.Test),nameof(R.Test1)).Test();

            _r.Test = nameof(R.Test);
            _r.Test1 = nameof(R.Test1);
            
            _testObserver.ItemCount.ShouldBe(1);
            
            
        }
        
        [Test][Order(30)]
        public void Emit_again_for_same_selected_properties_After_commit() {
            _r.ObjectSpace.CommitChanges();

            _r.Test = null;
            _r.Test1 = null;
            
            _testObserver.ItemCount.ShouldBe(2);
            
            
        }
        
        [Test][Order(40)]
        public void Do_not_Emit_for_non_selected_properties() {
            _r.ObjectSpace.CommitChanges();

            _r.Test3 = nameof(R.Test3);

            _testObserver.ItemCount.ShouldBe(2);
            
            
        }

    }
}