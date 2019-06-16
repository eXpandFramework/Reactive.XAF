using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.XtraGrid.Views.Grid;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xunit;

namespace Tests.Modules.ModelMapper{
    [Collection(nameof(XafTypesInfo))]
    public class ModelMapperExtenderServiceTests : ModelMapperBaseTest{
        [Theory]
        [InlineData(typeof(GridView),Platform.Win)]
        [InlineData(typeof(GridView),Platform.Web)]
        [InlineData(typeof(TestModelMapper),Platform.Win)]
        [InlineData(typeof(TestModelMapper),Platform.Web)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Win)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Web)]
        internal void ExtendModel(Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel)}{typeToMap.Name}{platform}");

            typeToMap.Extend<IModelListView>();

            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            modelListView.GetNode(mapName).ShouldNotBeNull();
            var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.FirstOrDefault();
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe($"IModel{mapName}");
            var defaultContext = ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(ModelMapperContextNodeGenerator.Default);
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
                modelListView.GetNode(mapName).GetNode(Xpand.XAF.Modules.ModelMapper.ModelMapperService.ModelMappersNodeName);
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
        [InlineData("Parent.AllowEdit=false", true,Platform.Win)]
        [InlineData("Parent.AllowEdit=true", false,Platform.Win)]
        [InlineData(null, true,Platform.Win)]
        internal void Container_Visibility(string visibilityCriteria, bool visibility,Platform platform){
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
