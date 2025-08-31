using System.Linq;
using System.Threading;
using DevExpress.ExpressApp.Editors;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.ViewItemValue;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
    public class ModelServiceTests:CommonTests{
        
        [Test][XpandTest()]
        public void DesignTemplateDetailViews_contains_views_With_RichEdit_PropertyEditor(){
            using var application=DocumentStyleManagerModule().Application;
            var templateDetailView = application.Model.DocumentStyleManager().DesignTemplateDetailViews.AddNode<IModelDesignTemplateDetailView>();
            templateDetailView.DetailViews.Count().ShouldBeGreaterThan(0);
            templateDetailView.DetailViews.All(view => view.MemberViewItems(typeof(IRichTextPropertyEditor)).Any()).ShouldBeTrue();

            var modelDesignTemplateContentEditor = templateDetailView.ContentEditors.AddNode<IModelDesignTemplateContentEditor>();
            modelDesignTemplateContentEditor.ContentEditors.Count().ShouldBe(0);
            templateDetailView.DetailView = application.Model.BOModel.GetClass(typeof(DataObject)).DefaultDetailView;
            modelDesignTemplateContentEditor.ContentEditors.Count().ShouldBeGreaterThan(0);
            modelDesignTemplateContentEditor.ContentEditors.All(editor => typeof(IRichTextPropertyEditor).IsAssignableFrom(editor.PropertyEditorType)).ShouldBeTrue();
        }

        [Test][XpandTest()]
        public void ImportStyles_should_lookup_classes_that_have_a_byte_array_property(){
            using var application=DocumentStyleManagerModule().Application;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
            var item = modelDocumentStyleManager.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
            var modelListViews = modelDocumentStyleManager.DocumentProviders.ToArray();
            
            modelListViews.Length.ShouldBeGreaterThanOrEqualTo(1);
            modelListViews.All(modelClass => modelClass.TypeInfo.Members.Any(info => info.MemberType == typeof(byte[]))).ShouldBeTrue();
        }
        [Test][XpandTest()]
        public void ImportStylesMember_default_value_should_be_the_first_byte_array_property(){
            using var application=DocumentStyleManagerModule().Application;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
            var item = modelDocumentStyleManager.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
            
	        item.Member.ShouldNotBeNull();
            
	        item.Member.Id().ShouldBe(nameof(DataObject.Content));
        }

        [Test][XpandTest()]
        public void Default_DefaultPropertiesProvider_should_be_the_ImportStylesModelClass(){
            using var application=DocumentStyleManagerModule().Application;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
            var item = modelDocumentStyleManager.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
	        
	        modelDocumentStyleManager.DefaultPropertiesProvider.ShouldBe(item.ModelClass);
        }

        [Test][XpandTest()]
        public void Default_DefaultPropertiesProviderModelMember_should_be_the_ImportStylesModelMember(){
            using var application=DocumentStyleManagerModule().Application;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
            var item = modelDocumentStyleManager.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
	        
	        modelDocumentStyleManager.DefaultPropertiesProviderMember.ShouldBe(item.Member);
        }

        [Test][XpandTest()]
        public void Default_DefaultPropertiesProviderModelMember_should_be_the_first_byte_array_property(){
            using var application=DocumentStyleManagerModule().Application;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
            modelDocumentStyleManager.DefaultPropertiesProvider = application.Model.BOModel.GetClass(typeof(DataObject));

	        modelDocumentStyleManager.DefaultPropertiesProviderMember.ShouldNotBeNull();
	        modelDocumentStyleManager.DefaultPropertiesProviderMember.Id().ShouldBe(nameof(DataObject.Content));
        }


        [TestCase( nameof(BusinessObjects.ApplyTemplateStyle)+"_DetailView",nameof(BusinessObjects.ApplyTemplateStyle.Template))]
        [TestCase( nameof(BusinessObjects.DocumentStyleManager)+"_DetailView",nameof(BusinessObjects.DocumentStyleManager.DocumentStyleLinkTemplate))]
        [XpandTest()][Apartment(ApartmentState.STA)]
        public void ViewItemValue_LookupDefaultObject(string objectView,string memberName){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelLookupDefaultObject = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>().ViewItemValue;

	        var modelLookupDefaultObjectItem = modelLookupDefaultObject.Items[application.Model.Views[objectView].Id];
	        modelLookupDefaultObjectItem.ShouldNotBeNull();
            modelLookupDefaultObjectItem.Members[memberName].ShouldNotBeNull();
        }

        [Test][XpandTest()]
        public void TemplateListViews_should_lookup_ListViews_that_have_a_byte_array_property(){
            using var application=DocumentStyleManagerModule().Application;
            var modelDocumentStyleManager = application.Model.DocumentStyleManager();
            var item = modelDocumentStyleManager.ApplyTemplateListViews.AddNode<IModelApplyTemplateListViewItem>();
	        
	        var modelListViews = item.ListViews.ToArray();

	        modelListViews.Length.ShouldBeGreaterThanOrEqualTo(1);
	        modelListViews.All(view => view.ModelClass.TypeInfo.Members.Any(info => info.MemberType == typeof(byte[]))).ShouldBeTrue();
        }

    }
}