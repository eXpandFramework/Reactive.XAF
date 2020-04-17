using System.Linq;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Model;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.LookupCascade.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.LookupCascade.Tests.ModelLogic{
    public class DatasourceModel:LookupCascadeBaseTest{
        [XpandTest]
        [TestCase("Order_ListView")]
        [TestCase("Order_DetailView")]
        public void LookupListView_Lookup_Lists_Views_With_Members_Using_the_cascade_Editor(string viewId){
            var application = ClientLookupCascadeModule(nameof(LookupListView_Lookup_Lists_Views_With_Members_Using_the_cascade_Editor)).Application;
            var applicationModel = application.Model;
        
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Product)).PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
            modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Accessory)).PropertyEditorType=typeof(ASPxLookupCascadePropertyEditor);
        
            var clientDatasourceLookupView = application.ReactiveModulesModel().LookupCascadeModel().Wait().ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>();
            clientDatasourceLookupView.LookupListViews.ShouldContain(applicationModel.FindLookupListView(typeof(Product)));
            clientDatasourceLookupView.LookupListViews.ShouldContain(applicationModel.FindLookupListView(typeof(Accessory)));
            
        }
    }
}