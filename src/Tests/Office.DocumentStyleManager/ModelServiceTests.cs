using System.Linq;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.ViewItemValue;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
    public class ModelServiceTests:BaseTests{
        
        [Test][XpandTest()]
        public void EnableStyleManager_ModelPropertyEditor_Attribute_Is_Visible_For_DetailView_With_RichEdit_PropertyEditor(){
	        using var application=DocumentStyleManagerModule().Application;
            var defaultDetailView = application.Model.BOModel.GetClass(typeof(DataObject)).DefaultDetailView;
            var modelPropertyEditor = defaultDetailView.Items.OfType<IModelPropertyEditor>().First(item => item.ModelMember.Name==nameof(DataObject.Content));
            modelPropertyEditor.IsPropertyVisible(nameof(IModelPropertyEditorEnableDocumentStyleManager.EnableDocumentStyleManager)).ShouldBeTrue();
            modelPropertyEditor = defaultDetailView.Items.OfType<IModelPropertyEditor>().First(item => item.ModelMember.Name==nameof(DataObject.Name));
            modelPropertyEditor.IsPropertyVisible(nameof(IModelPropertyEditorEnableDocumentStyleManager.EnableDocumentStyleManager)).ShouldBeFalse();   
        }

        [Test][XpandTest()]
        public void ImportStyles_should_lookup_classes_that_have_a_byte_array_property(){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelOffieModule = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule;
	        var item = modelOffieModule.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
            var modelListViews = modelOffieModule.DocumentProviders.ToArray();
            
            modelListViews.Length.ShouldBeGreaterThanOrEqualTo(1);
            modelListViews.All(modelClass => modelClass.TypeInfo.Members.Any(info => info.MemberType == typeof(byte[]))).ShouldBeTrue();
        }
        [Test][XpandTest()]
        public void ImportStylesMember_default_value_should_be_the_first_byte_array_property(){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelOffieModule = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule;
	        var item = modelOffieModule.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
            
	        item.Member.ShouldNotBeNull();
            
	        item.Member.Id().ShouldBe(nameof(DataObject.Content));
        }

        [Test][XpandTest()]
        public void Default_DefaultPropertiesProvider_should_be_the_ImportStylesModelClass(){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelOffieModule = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule;
	        var item = modelOffieModule.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
	        
	        modelOffieModule.DefaultPropertiesProvider.ShouldBe(item.ModelClass);
        }

        [Test][XpandTest()]
        public void Default_DefaultPropertiesProviderModelMember_should_be_the_ImportStylesModelMember(){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelOffieModule = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule;
	        var item = modelOffieModule.ImportStyles.AddNode<IModelImportStylesItem>();
	        item.ModelClass = application.Model.BOModel.GetClass(typeof(DataObject));
	        
	        modelOffieModule.DefaultPropertiesProviderMember.ShouldBe(item.Member);
        }

        [Test][XpandTest()]
        public void Default_DefaultPropertiesProviderModelMember_should_be_the_first_byte_array_property(){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelOffieModule = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule;
	        modelOffieModule.DefaultPropertiesProvider = application.Model.BOModel.GetClass(typeof(DataObject));

	        modelOffieModule.DefaultPropertiesProviderMember.ShouldNotBeNull();
	        modelOffieModule.DefaultPropertiesProviderMember.Id().ShouldBe(nameof(DataObject.Content));
        }


        [TestCase( nameof(BusinessObjects.ApplyTemplateStyle)+"_DetailView",nameof(BusinessObjects.ApplyTemplateStyle.Template))]
        [TestCase( nameof(BusinessObjects.DocumentStyleManager)+"_DetailView",nameof(BusinessObjects.DocumentStyleManager.DocumentStyleLinkTemplate))]
        [XpandTest()]
        public void LookupDefaultObject(string objectView,string memberName){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelLookupDefaultObject = application.Model.ToReactiveModule<IModelReactiveModulesViewItemValue>().ViewItemValue;

	        var modelLookupDefaultObjectItem = modelLookupDefaultObject.Items[application.Model.Views[objectView].Id];
	        modelLookupDefaultObjectItem.ShouldNotBeNull();
            modelLookupDefaultObjectItem.Members[memberName].ShouldNotBeNull();
        }

        [Test][XpandTest()]
        public void TemplateListvies_should_lookup_ListViews_that_have_a_byte_array_property(){
	        using var application=DocumentStyleManagerModule().Application;
	        var modelOffieModule = ((IModelOptionsOfficeModule) application.Model.Options).OfficeModule;
	        var item = modelOffieModule.ApplyTemplateListViews.AddNode<IModelApplyTemplateListViewItem>();
	        
	        var modelListViews = item.ListViews.ToArray();

	        modelListViews.Length.ShouldBeGreaterThanOrEqualTo(1);
	        modelListViews.All(view => view.ModelClass.TypeInfo.Members.Any(info => info.MemberType == typeof(byte[]))).ShouldBeTrue();
        }

    }
}