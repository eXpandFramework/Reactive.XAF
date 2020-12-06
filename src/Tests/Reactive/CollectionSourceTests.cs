using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    [NonParallelizable]
    public class CollectionSourceTests : ReactiveCommonTest{
        [XpandTest()][Test]
        public void When_Non_Persistent_DetailView_Nested_ListView_create_NonPersistentCollectionSource(){
            using (var application = DefaultReactiveModule().Application){
                var compositeView =(DetailView) application.NewView(application.FindDetailViewId(typeof(NonPersistentObject)));
                application.CreateViewWindow().SetView(compositeView);
                compositeView.GetListPropertyEditor<NonPersistentObject>(r => r.Childs).ListView.CollectionSource.GetType().ShouldBe(typeof(NonPersistePropertyCollectionSource));
            }

        }


    }
}