using System;
using System.Collections;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
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
    public class ExtendModelServiceTests : ModelMapperBaseTest{
        [Theory]
        [InlineData(typeof(GridView))]
        [InlineData(typeof(TestModelMapper))]
        [InlineData(typeof(SelfReferenceTypeProperties))]
        public void ExtendModel(Type typeToMap){
            InitializeMapperService($"{nameof(ExtendModel)}{typeToMap.Name}");

            typeToMap.MapToModel().Extend<IModelListView>();

            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            modelListView.GetNode(cleanCodeName).ShouldNotBeNull();
            var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.FirstOrDefault();
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe($"IModel{cleanCodeName}");
            var defaultContext =
                ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(
                    ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelMapper = defaultContext.GetNode(cleanCodeName);
            modelMapper.ShouldNotBeNull();

        }

        [Fact]
        public void ModelMapperContexts(){
            InitializeMapperService($"{nameof(ModelMapperContexts)}");
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel().Extend<IModelListView>();

            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelModelMappers =
                ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.First();

            modelModelMappers.Id().ShouldBe(ModelMapperContextNodeGenerator.Default);
            modelModelMappers.First().Id().ShouldBe(typeToMap.FullName.CleanCodeName());

        }

        [Fact]
        public void Container_ModelMapperContexts(){
            InitializeMapperService($"{nameof(Container_ModelMapperContexts)}");
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel().Extend<IModelListView>();

            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelMappersNode =
                modelListView.GetNode(cleanCodeName).GetNode(ModelMapperService.ModelMappersNodeName);
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
        [InlineData("Parent.AllowEdit=false", true)]
        [InlineData("Parent.AllowEdit=true", false)]
        [InlineData(null, true)]
        public void Container_Visibility(string visibilityCriteria, bool visibility){
            InitializeMapperService($"{nameof(Container_Visibility)}{visibilityCriteria.CleanCodeName()}");
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel(new ModelMapperConfiguration(){VisibilityCriteria = visibilityCriteria})
                .Extend<IModelListView>();

            var application = DefaultModelMapperModule(Platform.Win).Application;

            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            modelListView.IsPropertyVisible(cleanCodeName).ShouldBe(visibility);
        }

        public static ITypeInfo GetGenericListArgument(IModelNode nodeByPath){
            var type = nodeByPath.GetType();
            if (typeof(IEnumerable).IsAssignableFrom(type)){
                var genericModelList = type.GetInterfaces()
                    .First(type1 => typeof(IEnumerable).IsAssignableFrom(type1) && type1.IsGenericType);
                return XafTypesInfo.Instance.FindTypeInfo(genericModelList.GetGenericArguments()[0]);
            }

            return null;
        }


    }
}
