using System.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpace{
    public class ObjectSpaceExtensionsTests:ReactiveCommonAppTest{                                                   
        [Test]
        [XpandTest()]
        public void ShowPersistentObjectsInNonPersistentView(){
            
            using var objectSpace =Application.CreateObjectSpace();
            objectSpace.CreateObject<R>();
            objectSpace.CommitChanges();
            
            var listView = Application.NewView<ListView>(typeof(NonPersistentObject));
            
            listView.ObjectSpace.GetObjects<R>().ToArray().Length.ShouldBe(1);
        }
        [Test]
        [XpandTest()]
        public void WhenNewObjectCreated(){
            using var objectSpace = Application.CreateObjectSpace();
            var testObserver = objectSpace.WhenNewObjectCreated<R>().Test();
            
            objectSpace.CreateObject<R>();

            testObserver.ItemCount.ShouldBe(1);
        }

    }
}