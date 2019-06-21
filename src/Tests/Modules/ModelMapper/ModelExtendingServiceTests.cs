using System;
using System.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using Shouldly;
using Tests.Modules.ModelMapper.BOModel;
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
        private const string MMListViewNodePath = "Views/" + nameof(MM) + "_ListView";
        [Theory]
        [InlineData(typeof(TestModelMapper),Platform.Win)]
        [InlineData(typeof(TestModelMapper),Platform.Web)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Win)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Web)]
        internal void ExtendModel_Any_Type(Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_Any_Type)}{typeToMap.Name}{platform}");

            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            AssertExtendedModel(typeToMap, application, MMListViewNodePath);
        }

        [Theory]
        [InlineData(PredifinedModelMapperConfiguration.GridColumn, typeof(GridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedModelMapperConfiguration.GridView, typeof(GridView),Platform.Win,MMListViewNodePath)]
        internal void ExtendModel_Predefined_Type(PredifinedModelMapperConfiguration configuration,Type typeToMap,Platform platform,string nodePath){
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_Multiple_Predefined_Type)}{configuration}{platform}",platform);

            configuration.Extend();
            var application = DefaultModelMapperModule(platform).Application;
            AssertExtendedModel(typeToMap, application,nodePath);
        }

        [Fact]
        internal void ExtendModel_Multiple_Predefined_Type(){
            Assembly.LoadFile(typeof(GridView).Assembly.Location);
            var platform = Platform.Win;
            InitializeMapperService($"{nameof(ExtendModel_Multiple_Predefined_Type)}",platform);

            var configuration = PredifinedModelMapperConfiguration.GridView|PredifinedModelMapperConfiguration.GridColumn;
            configuration.Extend();
            var application = DefaultModelMapperModule(platform).Application;
            AssertExtendedModel(typeof(GridView), application, MMListViewNodePath);
            AssertExtendedModel(typeof(GridColumn), application, $"{MMListViewNodePath}/Columns/Test");
        }

        private void AssertExtendedModel(Type typeToMap, XafApplication application,string nodePath){
            
            var modelNode = application.Model.GetNodeByPath(nodePath);
            var mapName = typeToMap.ModelMapName();
            modelNode.GetNode(mapName).ShouldNotBeNull();
            var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.FirstOrDefault(info => info.Name.EndsWith(typeToMap.Name));
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe($"IModel{mapName}");
            var defaultContext =
                ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(
                    ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelMapper = defaultContext.GetNode(mapName);
            modelMapper.ShouldNotBeNull();
        }

        [Fact]
        public void Extend_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService(nameof(Extend_Multiple_Objects_with_common_types));

            typeToMap1.Extend<IModelListView>();
            typeToMap2.Extend<IModelColumn>();

            var application = DefaultModelMapperModule(Platform.Win).Application;
            var appearanceCell = application.Model.GetNodeByPath($@"{MMListViewNodePath}/Columns/Test/{nameof(TestModelMapperCommonType2)}/{nameof(TestModelMapperCommonType2.AppearanceCell)}");
            appearanceCell.ShouldNotBeNull();
            appearanceCell.GetNodeByPath($"{nameof(AppearanceObjectEx.TextOptions)}");
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
                modelListView.GetNode(mapName).GetNode(TypeMappingService.ModelMappersNodeName);
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
        [InlineData("Parent.AllowEdit=?", true,false,null)]
        [InlineData("Parent.AllowEdit=?", false,true,null)]
        [InlineData(VisibilityCriteriaLeftOperand.IsAssignableFromModelListVideEditorType, true,typeof(WinColumnsListEditor),"Parent.")]
        [InlineData("Parent."+nameof(IModelListView.EditorType)+"=?", true,typeof(GridListEditor),null)]
        [InlineData(null, true,null,null)]
        internal void Container_Visibility(object leftOperand, bool visibility,object rightOperand,string path){
            var visibilityCriteria = $"{CriteriaOperator.Parse($"{leftOperand}", rightOperand)}";
            if (leftOperand is VisibilityCriteriaLeftOperand visibilityCriteriaLeftOperand){
                visibilityCriteria = visibilityCriteriaLeftOperand.GetVisibilityCriteria(rightOperand,path);
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
