using System.Linq;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public class ObjectSpaceExtensionsTests:ReactiveCommonTest{                                                   
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