using System;
using System.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraGrid.Views.Grid;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.ObjectMapping;
using Xunit;

namespace Tests.Modules.ModelMapper{
    [Collection(nameof(XafTypesInfo))]
    public class ModelMapperExtenderServiceTests : ModelMapperBaseTest{
        [Theory]
        [InlineData(typeof(TestModelMapper),Platform.Win)]
        [InlineData(typeof(TestModelMapper),Platform.Web)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Win)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Web)]
        internal void ExtendModel_Any_Type(Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_Any_Type)}{typeToMap.Name}{platform}");

            typeToMap.Extend<IModelListView>();

            AssertExtendedModel(typeToMap, platform);
        }

        [Theory]
        [InlineData(PredifinedModelMapperConfiguration.GridView, typeof(GridView),Platform.Win)]
        internal void ExtendModel_Prededined_Type(PredifinedModelMapperConfiguration configuration,Type typeToMap,Platform platform){
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_Prededined_Type)}{configuration}{platform}",platform);

            configuration.Extend();

            AssertExtendedModel(typeToMap, platform);
        }

        private void AssertExtendedModel(Type typeToMap, Platform platform){
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            modelListView.GetNode(mapName).ShouldNotBeNull();
            var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.FirstOrDefault();
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe($"IModel{mapName}");
            var defaultContext =
                ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(
                    ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelMapper = defaultContext.GetNode(mapName);
            modelMapper.ShouldNotBeNull();
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void ModelMapperContexts(Platform platform){
            InitializeMapperService($"{nameof(ModelMapperContexts)}{platform}");
            var typeToMap = typeof(TestModelMapper);

            typeToMap.Extend<IModelListView>();

            var application = DefaultModelMapperModule(platform).Application;
            var modelModelMappers = ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.First();
            modelModelMappers.Id().ShouldBe(ModelMapperContextNodeGenerator.Default);
            modelModelMappers.First().Id().ShouldBe(typeToMap.ModelMapName());
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Container_ModelMapperContexts(Platform platform){
            InitializeMapperService($"{nameof(Container_ModelMapperContexts)}{platform}");
            var typeToMap = typeof(TestModelMapper);

            typeToMap.Extend<IModelListView>();

            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelMappersNode =
                modelListView.GetNode(mapName).GetNode(ObjectMappingService.ModelMappersNodeName);
            modelMappersNode.ShouldNotBeNull();
            modelMappersNode.Index.ShouldBe(0);
            var defaultContext = modelMappersNode.GetNode(ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelModelMappers =
                defaultContext.GetValue<IModelModelMappers>(nameof(IModelMapperContextContainer.Context));
            modelModelMappers.ShouldNotBeNull();
            modelModelMappers.ShouldBe(
                ((IModelApplicationModelMapper) modelModelMappers.Application).ModelMapper.MapperContexts[
                    ModelMapperContextNodeGenerator.Default]);
        }

        [Theory]
        [InlineData("Parent.AllowEdit=?", true,false)]
        [InlineData("Parent.AllowEdit=?", false,true)]
        [InlineData(VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType, true,typeof(WinColumnsListEditor))]
        [InlineData("Parent."+nameof(IModelListView.EditorType)+"=?", true,typeof(GridListEditor))]
        [InlineData(null, true,null)]
        internal void Container_Visibility(object leftOperand, bool visibility,object rightOperand){
            var visibilityCriteria = $"{CriteriaOperator.Parse($"{leftOperand}", rightOperand)}";
            if (leftOperand is VisibilityCriteriaLeftOperand visibilityCriteriaLeftOperand){
                visibilityCriteria = visibilityCriteriaLeftOperand.GetVisibilityCriteria(rightOperand);
            }

            var platform = Platform.Win;
            InitializeMapperService($"{nameof(Container_Visibility)}{visibilityCriteria.CleanCodeName()}{platform}");
            var typeToMap = typeof(TestModelMapper);

            typeToMap.Extend<IModelListView>(new ModelMapperConfiguration(){VisibilityCriteria = visibilityCriteria});

            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelMapName = typeToMap.ModelMapName();
            modelListView.IsPropertyVisible(modelMapName).ShouldBe(visibility);
            
        }



    }
}
