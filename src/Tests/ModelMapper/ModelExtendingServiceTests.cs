using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Utils;
using DevExpress.Web;
using DevExpress.Web.ASPxHtmlEditor;
using DevExpress.Web.ASPxScheduler;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraLayout;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using EnumsNET;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predifined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    [Collection(nameof(ModelMapperModule))]
    public class ModelMapperExtenderServiceTests : ModelMapperBaseTest{

        [Theory]
        [InlineData(typeof(TestModelMapper),Platform.Win)]
        [InlineData(typeof(TestModelMapper),Platform.Web)]
        [InlineData(typeof(RootType),Platform.Win)]
        [InlineData(typeof(RootType),Platform.Web)]
        internal void ExtendModel_Any_Type(Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_Any_Type)}{typeToMap.Name}{platform}");

            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            AssertExtendedListViewModel(typeToMap, application, MMListViewNodePath);
        }

        [Theory]
//        [InlineData(PredifinedMap.GridColumn, typeof(GridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
//        [InlineData(PredifinedMap.GridView, typeof(GridView),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.SchedulerControl, typeof(SchedulerControl),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.PivotGridControl, typeof(PivotGridControl),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.ChartControl, typeof(ChartControl),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.PivotGridField, typeof(PivotGridField),Platform.Win,MMListViewNodePath+"/Columns/Test")]
//        [InlineData(PredifinedMap.LayoutViewColumn, typeof(LayoutViewColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
//        [InlineData(PredifinedMap.LayoutView, typeof(LayoutView),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.BandedGridColumn, typeof(BandedGridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
//        [InlineData(PredifinedMap.AdvBandedGridView, typeof(AdvBandedGridView),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.GridViewColumn, typeof(GridViewColumn),Platform.Web,MMListViewNodePath+"/Columns/Test")]
//        [InlineData(PredifinedMap.ASPxGridView, typeof(ASPxGridView),Platform.Web,MMListViewNodePath)]
//        [InlineData(PredifinedMap.ASPxHtmlEditor, typeof(ASPxHtmlEditor),Platform.Web,MMDetailViewTestItemNodePath)]
//        [InlineData(PredifinedMap.TreeList, typeof(TreeList),Platform.Win,MMListViewNodePath)]
//        [InlineData(PredifinedMap.TreeListColumn, typeof(TreeListColumn),Platform.Win,MMListViewTestItemNodePath)]
//        [InlineData(PredifinedMap.ASPxScheduler, typeof(ASPxScheduler),Platform.Web,MMListViewNodePath)]
//        [InlineData(PredifinedMap.XafLayoutControl, typeof(XafLayoutControl),Platform.Win,MMDetailViewNodePath)]
        [InlineData(PredifinedMap.SplitContainerControl, typeof(SplitContainerControl),Platform.Win,MMListViewNodePath+"/SplitLayout")]
        internal void ExtendModel_Predefined_Type(PredifinedMap configuration,Type typeToMap,Platform platform,string nodePath){
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_Predefined_Type)}{configuration}{platform}",platform);

            var module = configuration.Extend();
            var application = DefaultModelMapperModule(platform,module).Application;
            AssertExtendedListViewModel(typeToMap, application,nodePath);
        }

        private void AssertExtendedListViewModel(Type typeToMap, XafApplication application,string nodePath){
            
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
        internal void Extend_PredifinedRepositoryItems(){

            var predifinedMaps = Enums.GetValues<PredifinedMap>()
                .Where(map => map.IsRepositoryItem())
//                .Where(map => map==PredifinedMap.RepositoryItemProtectedContentTextEdit)
                ;
//                .Where(map => map == PredifinedMap.RepositoryItemMarqueeProgressBar);
//                .Where(map => new[]{PredifinedMap.RepositoryItem,PredifinedMap.RepositoryItemButtonEdit}.Contains(map));
            foreach (var predifinedMap in predifinedMaps){
                try{
                    InitializeMapperService($"{nameof(Extend_PredifinedRepositoryItems)}{predifinedMap}",Platform.Win);
                    var module = predifinedMap.Extend();
                    var application = DefaultModelMapperModule(Platform.Win,module).Application;
                    var typeToMap = predifinedMap.GetTypeToMap();
                    
                    var modelNode = application.Model.GetNodeByPath(MMDetailViewTestItemNodePath);
                    var mapName = RepositoryItemService.MapName;
                    modelNode.GetNode(mapName).ShouldNotBeNull();
                    modelNode = application.Model.GetNodeByPath(MMListViewTestItemNodePath);
                    modelNode.GetNode(mapName).ShouldNotBeNull();
                    var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.FirstOrDefault(info => info.Name.EndsWith(typeToMap.Name));
                    typeInfo.ShouldNotBeNull();
                    typeInfo.Name.ShouldBe($"IModel{typeToMap.ModelMapName()}");
                    
                    var defaultContext =((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(ModelMapperContextNodeGenerator.Default);
                    defaultContext.ShouldNotBeNull();
                    var modelMapper = defaultContext.GetNode(predifinedMap.DisplayName());
                    modelMapper.ShouldNotBeNull();
                    application.Dispose();
                    Dispose();
                }
                catch (Exception e){
                    throw new Exception(predifinedMap.ToString(), e);
                }
            }
        }

        [Theory]
        [InlineData(PredifinedMap.ChartControlRadarDiagram, typeof(RadarDiagram),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlPolarDiagram, typeof(PolarDiagram),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlXYDiagram2D, typeof(XYDiagram2D),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlXYDiagram, typeof(XYDiagram),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlSwiftPlotDiagram, typeof(SwiftPlotDiagram),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlGanttDiagram, typeof(GanttDiagram),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlFunnelDiagram3D, typeof(FunnelDiagram3D),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlDiagram3D, typeof(Diagram3D),Platform.Win)]
        [InlineData(PredifinedMap.ChartControlSimpleDiagram3D, typeof(SimpleDiagram3D),Platform.Win)]
        internal void ExtendModel_PredefinedChartDiagram(PredifinedMap configuration,Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_PredefinedChartDiagram)}{configuration}{platform}",platform);

            var module = PredifinedMap.ChartControl.Extend();
            configuration.Extend(module);
            var application = DefaultModelMapperModule(platform,module).Application;

            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelNode = modelListView.GetNode(PredifinedMap.ChartControl.ToString());
            modelNode= modelNode.GetNode("Diagrams");
            var diagramType = modelNode.GetType().GetInterfaces().First(type =>type.IsGenericType&& type.GetGenericTypeDefinition()==typeof(IModelList<>)).GetGenericArguments().First();
            var targetType = diagramType.Assembly.GetType($"IModel{configuration.ToString().Replace(PredifinedMap.ChartControl.ToString(),"")}");
            diagramType.IsAssignableFrom(targetType).ShouldBeTrue();
        }

        [Fact]
        internal void ExtendModel_All_PredefinedChartDiagram(){
            Assembly.LoadFile(typeof(ChartControl).Assembly.Location);
            Assembly.LoadFile(typeof(Diagram).Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_All_PredefinedChartDiagram)}",Platform.Win);

            var module = PredifinedMap.ChartControl.Extend();
            var diagrams = Enums.GetMembers<PredifinedMap>().Where(member =>
                member.Name.StartsWith(PredifinedMap.ChartControl.ToString()) &&
                member.Value != PredifinedMap.ChartControl&&member.Value != PredifinedMap.ChartControlDiagram).Select(member => member.Value).ToArray();
            diagrams.Extend(module);
            DefaultModelMapperModule(Platform.Win,module);
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void ExtendModel_All_Predefined_Maps(Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_All_Predefined_Maps)}",platform);
            var values = Enums.GetValues<PredifinedMap>()
                .Where(map =>map.GetAttributes().OfType<MapPlatformAttribute>().Any(_ => _.Platform == platform.ToString()))
                .ToArray();

            var module = values.ToArray().Extend();

            DefaultModelMapperModule(platform,module);

        }

        [Fact]
        public void Extend_Existing_PredifinedMap(){
            InitializeMapperService(nameof(Extend_Existing_PredifinedMap),Platform.Win);
            var module = new []{PredifinedMap.PivotGridControl,PredifinedMap.GridView}.Extend();

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



    }
}
