using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Utils;
using DevExpress.Web;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraPivotGrid;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    [Collection(nameof(ModelMapperModule))]
    public class ModelMapperExtenderServiceTests : ModelMapperBaseTest{
        
        [Theory]
        [InlineData(typeof(TestModelMapper),Platform.Win)]
        [InlineData(typeof(TestModelMapper),Platform.Web)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Win)]
        [InlineData(typeof(SelfReferenceTypeProperties),Platform.Web)]
        internal void ExtendModel_Any_Type(Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_Any_Type)}{typeToMap.Name}{platform}");

            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            AssertExtendedModel(typeToMap, application, MMListViewNodePath);
        }

        [Theory]
        [InlineData(PredifinedMap.GridColumn, typeof(GridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.GridView, typeof(GridView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.PivotGridControl, typeof(PivotGridControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.PivotGridField, typeof(PivotGridField),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.LayoutViewColumn, typeof(LayoutViewColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.LayoutView, typeof(LayoutView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.BandedGridColumn, typeof(BandedGridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.AdvBandedGridView, typeof(AdvBandedGridView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.GridViewColumn, typeof(GridViewColumn),Platform.Web,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.ASPxGridView, typeof(ASPxGridView),Platform.Web,MMListViewNodePath)]
        internal void ExtendModel_Predefined_Type(PredifinedMap configuration,Type typeToMap,Platform platform,string nodePath){
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_Multiple_Predefined_Type)}{configuration}{platform}",platform);
            ConfigureLayoutViewPredifinedMapService(configuration);

            var module = configuration.Extend();
            var application = DefaultModelMapperModule(platform,module).Application;
            AssertExtendedModel(typeToMap, application,nodePath);
        }

        [Fact]
        internal void ExtendModel_Multiple_Predefined_Type(){
            Assembly.LoadFile(typeof(GridView).Assembly.Location);
            var platform = Platform.Win;
            InitializeMapperService($"{nameof(ExtendModel_Multiple_Predefined_Type)}",platform);


            var module = new[]{PredifinedMap.GridView,PredifinedMap.GridColumn}.Extend();

            var application = DefaultModelMapperModule(platform,module).Application;
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
        public async Task Extend_Existing_PredifinedMap(){
            InitializeMapperService(nameof(Extend_Existing_PredifinedMap),Platform.Win);
            var module = PredifinedMap.GridView.Extend();
            module.ApplicationModulesManager
                .FirstAsync()
                .SelectMany(_ => _.ExtendMap(PredifinedMap.GridView))
                .Subscribe(_ => {
                    _.extenders.Add(_.targetInterface,typeof(IModelPredifinedMapExtension));
                });
            var application = DefaultModelMapperModule(Platform.Win,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelNode = modelListView.GetNode(typeof(GridView).ModelMapName());

            (modelNode is IModelPredifinedMapExtension).ShouldBeTrue();
            
        }

        [Fact]
        public void Extend_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService(nameof(Extend_Multiple_Objects_with_common_types));

            var module = new ModelMapperTestModule();
            typeToMap1.Extend<IModelListView>(module);
            typeToMap2.Extend<IModelColumn>(module);

            var application = DefaultModelMapperModule(Platform.Win,module).Application;
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

            var module = typeToMap.Extend<IModelListView>();

            var application = DefaultModelMapperModule(platform,module).Application;
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

            var module = typeToMap.Extend<IModelListView>();

            var application = DefaultModelMapperModule(platform,module).Application;
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

            var module = typeToMap.Extend<IModelListView>(null,new ModelMapperConfiguration(){VisibilityCriteria = visibilityCriteria});

            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelMapName = typeToMap.ModelMapName();
            modelListView.IsPropertyVisible(modelMapName).ShouldBe(visibility);
            
        }


        [Fact]
        internal  void LayoutView_LayoutStore(){
            var typeToExtend = typeof(LayoutView);
            Assembly.LoadFile(typeToExtend.Assembly.Location);
            InitializeMapperService($"{nameof(LayoutView_LayoutStore)}",Platform.Win);
            ConfigureLayoutViewPredifinedMapService();

            var module = PredifinedMap.LayoutView.Extend();
            var application = DefaultModelMapperModule(Platform.Win,module).Application;

            var modelListView = application.Model.Views.OfType<IModelListView>().First();

            var modelMap = modelListView.GetNode(typeToExtend.ModelMapName());
            modelMap.ShouldBeAssignableTo<IModelDesignLayoutView>();
            modelMap.GetNode(nameof(IModelDesignLayoutView.DesignLayoutView)).ShouldNotBeNull();
        }

    }
}
