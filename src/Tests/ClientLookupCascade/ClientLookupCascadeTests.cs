using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppDomainToolkit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Model;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.ClientLookupCascade.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ClientLookupCascade.Tests{
    [NonParallelizable]
    [Serializable]
    public class ReactiveLoggerTests : BaseTest{
        [XpandTest]
        [TestCase("Project_ListView")]
        [TestCase("Project_DetailView")]
        public void Client_Datasource_Model_LookupListView_Lookup(string viewId){
            var application = ClientLookupCascadeModule(nameof(Client_Datasource_Model_LookupListView_Lookup)).Application;
            var applicationModel = application.Model;

            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Customer)).PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);
            modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Order)).PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);

            var clientDatasourceLookupView = ((IModelOptionsClientDatasource) applicationModel.Options).ClientDatasource.LookupViews.AddNode<IModelClientDatasourceLookupView>("Project");
            clientDatasourceLookupView.LookupListViews.ShouldContain(applicationModel.FindLookupListView(typeof(Customer)));
            clientDatasourceLookupView.LookupListViews.ShouldContain(applicationModel.FindLookupListView(typeof(Order)));
            
        }
        [XpandTest]
        [TestCase("Project_ListView")]
        [TestCase("Project_DetailView")]
        public void PropertyEditor_Model_Visibility(string viewId){
            var application = ClientLookupCascadeModule(nameof(PropertyEditor_Model_Visibility)).Application;
            var applicationModel = application.Model;
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            var customerModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Customer));
            customerModelMemberViewItem.PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);
            var orderModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Order));
            orderModelMemberViewItem.PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);

            customerModelMemberViewItem.IsPropertyVisible(nameof(IModelMemberViewItemASPxClientLookupPropertyEditor.ASPxClientLookupPropertyEditor)).ShouldBe(true);
            orderModelMemberViewItem.IsPropertyVisible(nameof(IModelMemberViewItemASPxClientLookupPropertyEditor.ASPxClientLookupPropertyEditor)).ShouldBe(true);
            
            
        }
        [XpandTest]
        [TestCase("Project_ListView")]
        [TestCase("Project_DetailView")]
        public void PropertyEditor_Model_Cascade_and_Synchronize_MemberViewItems_Lookups(string viewId){
            var application = ClientLookupCascadeModule(nameof(PropertyEditor_Model_Cascade_and_Synchronize_MemberViewItems_Lookups)).Application;
            var applicationModel = application.Model;
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            var customerModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Customer));
            customerModelMemberViewItem.PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);
            var orderModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Order));
            orderModelMemberViewItem.PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);

            var customerPropertyEditor = ((IModelMemberViewItemASPxClientLookupPropertyEditor) customerModelMemberViewItem).ASPxClientLookupPropertyEditor;
            customerPropertyEditor.LookupPropertyEditorMemberViewItems.ShouldContain(modelMemberViewItems.First(item => item.Id==nameof(Project.Order)));
            customerPropertyEditor.LookupPropertyEditorMemberViewItems.Count().ShouldBe(1);
            
            var orderPropertyEditor = ((IModelMemberViewItemASPxClientLookupPropertyEditor) orderModelMemberViewItem).ASPxClientLookupPropertyEditor;
            orderPropertyEditor.LookupPropertyEditorMemberViewItems.ShouldContain(modelMemberViewItems.First(item => item.Id==nameof(Project.Customer)));
            orderPropertyEditor.LookupPropertyEditorMemberViewItems.Count().ShouldBe(1);
        }
        
        [XpandTest]
        [TestCase("Project_ListView")]
        [TestCase("Project_DetailView")]
        public void PropertyEditor_Model_CascadeColumnFilter_and_SynchronizeMemberLookupColumn_Lookups(string viewId){
            var application = ClientLookupCascadeModule(nameof(PropertyEditor_Model_CascadeColumnFilter_and_SynchronizeMemberLookupColumn_Lookups)).Application;
            var applicationModel = application.Model;
            var modelObjectView = applicationModel.Views[viewId].AsObjectView;
            var modelMemberViewItems = modelObjectView.MemberViewItems();
            var customerModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Customer));
            customerModelMemberViewItem.PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);
            var orderModelMemberViewItem = modelMemberViewItems.First(item => item.ModelMember.MemberInfo.MemberType==typeof(Order));
            orderModelMemberViewItem.PropertyEditorType=typeof(ASPxClientLookupCascadePropertyEditor);

            var customerPropertyEditor = ((IModelMemberViewItemASPxClientLookupPropertyEditor) customerModelMemberViewItem).ASPxClientLookupPropertyEditor;
            customerPropertyEditor.CascadeMemberViewItem=modelMemberViewItems.First(item => item.Id==nameof(Project.Order));
            customerPropertyEditor.CascadeColumnFilters.ShouldContain(applicationModel.FindLookupListView(typeof(Order)).Columns[nameof(Order.OrderName)]);
            customerPropertyEditor.CascadeColumnFilters.Count().ShouldBe(1);
            customerPropertyEditor.SynchronizeMemberViewItem=modelMemberViewItems.First(item => item.Id==nameof(Project.Customer));
            customerPropertyEditor.SynchronizeMemberLookupColumns.ShouldContain(applicationModel.FindLookupListView(typeof(Customer)).Columns[nameof(Customer.CustomerName)]);
            customerPropertyEditor.SynchronizeMemberLookupColumns.Count().ShouldBe(1);
            
            var orderPropertyEditor = ((IModelMemberViewItemASPxClientLookupPropertyEditor) customerModelMemberViewItem).ASPxClientLookupPropertyEditor;
            orderPropertyEditor.CascadeMemberViewItem=modelMemberViewItems.First(item => item.Id==nameof(Project.Customer));
            orderPropertyEditor.CascadeColumnFilters.ShouldContain(applicationModel.FindLookupListView(typeof(Customer)).Columns[nameof(Customer.CustomerName)]);
            orderPropertyEditor.CascadeColumnFilters.Count().ShouldBe(1);
            orderPropertyEditor.SynchronizeMemberViewItem=modelMemberViewItems.First(item => item.Id==nameof(Project.Order));
            orderPropertyEditor.SynchronizeMemberLookupColumns.ShouldContain(applicationModel.FindLookupListView(typeof(Order)).Columns[nameof(Order.OrderName)]);
            orderPropertyEditor.SynchronizeMemberLookupColumns.Count().ShouldBe(1);
        }
        
         ClientLookupCascadeModule ClientLookupCascadeModule(string title,params ModuleBase[] modules){
            var xafApplication = Platform.Win.NewApplication<ClientLookupCascadeModule>();
            xafApplication.Title = title;
            xafApplication.Modules.AddRange(modules);
            var module = xafApplication.AddModule<ClientLookupCascadeModule>(typeof(Customer),typeof(Order),typeof(Project));
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return module.Application.Modules.OfType<ClientLookupCascadeModule>().First();
        }
    }
}