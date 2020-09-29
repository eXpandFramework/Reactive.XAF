using System.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ObjectSpaceExtensionsTests:ReactiveBaseTest{
        [TestCase(true)]
        [TestCase(false)]
        [XpandTest()]
        public void NewObjects(bool emitOnCreation){
            using (var application = DefaultReactiveModule().Application){
                var testObserver = application.NewObject<R>(emitOnCreation).Test();
                var objectSpace = application.CreateObjectSpace();
                var o = objectSpace.CreateObject<R>();
                if (!emitOnCreation){
                    objectSpace.CommitChanges();   
                }

                testObserver.Items.Count.ShouldBe(1);
                testObserver.Items.First().theObject.ShouldBe(o);
            }

        }

        [Test]
        [XpandTest()]
        public void ShowPersistentObjectsInNonPersistentView(){
            using var application = DefaultReactiveModule().Application;
            using var objectSpace = application.CreateObjectSpace();
            objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            
            var listView = application.NewView<ListView>(typeof(NonPersistentObject));
            
            listView.ObjectSpace.GetObjects<R>().ToArray().Length.ShouldBe(1);
        }

    }
}