using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.DashboardWeb;
using DevExpress.DashboardWin;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Utils;
using DevExpress.Web;
using DevExpress.Web.ASPxScheduler;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using EnumsNET;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.TypesInfo;
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

        [Fact]
        internal void Customize_PredifienedMaps_TargetInterface(){
            InitializeMapperService($"{nameof(Customize_PredifienedMaps_TargetInterface)}",Platform.Win);
            
            var module = PredifinedMap.DashboardDesigner.Extend(null,configuration => {
                configuration.TargetInterfaceTypes.Clear();
                configuration.TargetInterfaceTypes.Add(typeof(IModelOptions));
            });
            var application = DefaultModelMapperModule(Platform.Win,module).Application;

            application.Model.Options.GetNode(PredifinedMap.DashboardDesigner).ShouldNotBeNull();
        }

        [Theory]
        [InlineData(PredifinedMap.GridColumn, typeof(GridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.GridView, typeof(GridView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.SchedulerControl, typeof(SchedulerControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.PivotGridControl, typeof(PivotGridControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.ChartControl, typeof(ChartControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.PivotGridField, typeof(PivotGridField),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.LayoutViewColumn, typeof(LayoutViewColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.LayoutView, typeof(LayoutView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.BandedGridColumn, typeof(BandedGridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.AdvBandedGridView, typeof(AdvBandedGridView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredifinedMap.GridViewColumn, typeof(GridViewColumn),Platform.Web,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredifinedMap.ASPxGridView, typeof(ASPxGridView),Platform.Web,MMListViewNodePath)]
        [InlineData(PredifinedMap.TreeList, typeof(TreeList),Platform.Win,MMListViewNodePath+",NavigationItems")]
        [InlineData(PredifinedMap.TreeListColumn, typeof(TreeListColumn),Platform.Win,MMListViewTestItemNodePath)]
        [InlineData(PredifinedMap.ASPxScheduler, typeof(ASPxScheduler),Platform.Web,MMListViewNodePath)]
        [InlineData(PredifinedMap.XafLayoutControl, typeof(XafLayoutControl),Platform.Win,MMDetailViewNodePath)]
        [InlineData(PredifinedMap.SplitContainerControl, typeof(SplitContainerControl),Platform.Win,MMListViewNodePath+"/SplitLayout")]
        [InlineData(PredifinedMap.DashboardDesigner, typeof(DashboardDesigner),Platform.Win,MMDetailViewTestItemNodePath)]
        [InlineData(PredifinedMap.ASPxPopupControl, typeof(ASPxPopupControl),Platform.Web,MMListViewNodePath+","+MMDetailViewNodePath)]
        internal void ExtendModel_Predefined_Type(PredifinedMap configuration,Type typeToMap,Platform platform,string nodePath){
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_Predefined_Type)}{configuration}{platform}",platform);

            var module = configuration.Extend();
            var application = DefaultModelMapperModule(platform,module).Application;
            AssertExtendedListViewModel(typeToMap, application,nodePath);
        }

        private void AssertExtendedListViewModel(Type typeToMap, XafApplication application,string nodePath){
            var mapName = typeToMap.ModelTypeName();
            foreach (var s in nodePath.Split(',')){
                var modelNode = application.Model.GetNodeByPath(s);
                modelNode.GetNode(typeToMap.Name).ShouldNotBeNull();
            }
            
            var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants.FirstOrDefault(info => info.Name.EndsWith(typeToMap.Name));
            typeInfo.ShouldNotBeNull();
            typeInfo.Name.ShouldBe(mapName);
            var defaultContext =((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(ModelMapperContextNodeGenerator.Default);
            defaultContext.ShouldNotBeNull();
            var modelMapper = defaultContext.GetNode(typeToMap.Name);
            modelMapper.ShouldNotBeNull();
        }

        [Fact]
        internal void Extend_PredifinedRepositoryItems(){
            var predifinedMaps = Enums.GetValues<PredifinedMap>()
                .Where(map => map.IsRepositoryItem());
            Extend_Predifiened_ViewItems(predifinedMaps,Platform.Win, ViewItemService.RepositoryItemsMapName,true);
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Extend_Predifined_PropertyEditorControls(Platform platform){
            var predifinedMaps = Enums.GetValues<PredifinedMap>()
                .Where(map => map.IsPropertyEditor()&&map.Attribute<MapPlatformAttribute>().Platform==platform.ToString());
            new[]{typeof(ASPxDashboard)}.ToObservable().Subscribe();
            Extend_Predifiened_ViewItems(predifinedMaps,platform, ViewItemService.PropertyEditorControlMapName);
        }

        private void Extend_Predifiened_ViewItems(IEnumerable<PredifinedMap> predifinedMaps, Platform platform,string mapPropertyName,bool checkListViewColumns=false){
            foreach (var predifinedMap in predifinedMaps){
                try{
                    InitializeMapperService($"{nameof(Extend_Predifiened_ViewItems)}{predifinedMap}", platform);
                    var module = predifinedMap.Extend();
                    var connectableObservable = TypeMappingService.MappedTypes.Replay();
                    connectableObservable.Connect();
                    var application = DefaultModelMapperModule(platform, module).Application;
                    var typeToMap = predifinedMap.TypeToMap();
                    
                    var modelNode = application.Model.GetNodeByPath(MMDetailViewTestItemNodePath);
                    
                    modelNode.GetNode(mapPropertyName).ShouldNotBeNull();
                    if (checkListViewColumns){
                        modelNode = application.Model.GetNodeByPath(MMListViewTestItemNodePath);
                        modelNode.GetNode(mapPropertyName).ShouldNotBeNull();
                    }
                    var typeInfo = XafTypesInfo.Instance.FindTypeInfo(typeof(IModelModelMap)).Descendants
                        .FirstOrDefault(info => info.Name.EndsWith(typeToMap.Name));
                    typeInfo.ShouldNotBeNull();
                    typeInfo.Name.ShouldBe(typeToMap.ModelTypeName());

                    var defaultContext =
                        ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.GetNode(
                            ModelMapperContextNodeGenerator.Default);
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
//        [InlineData(PredifinedMap.ChartControlPolarDiagram, typeof(PolarDiagram),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlXYDiagram2D, typeof(XYDiagram2D),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlXYDiagram, typeof(XYDiagram),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlSwiftPlotDiagram, typeof(SwiftPlotDiagram),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlGanttDiagram, typeof(GanttDiagram),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlFunnelDiagram3D, typeof(FunnelDiagram3D),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlDiagram3D, typeof(Diagram3D),Platform.Win)]
//        [InlineData(PredifinedMap.ChartControlSimpleDiagram3D, typeof(SimpleDiagram3D),Platform.Win)]
        internal void ExtendModel_PredefinedChartDiagram(PredifinedMap configuration,Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_PredefinedChartDiagram)}{configuration}{platform}",platform);

            var module = PredifinedMap.ChartControl.Extend();
            configuration.Extend(module);
            var application = DefaultModelMapperModule(platform,module).Application;

            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelNode = modelListView.GetNode(PredifinedMap.ChartControl);
            modelNode= modelNode.GetNode("Diagrams");

            var diagramType = modelNode.ModelListItemType();
            var targetType = diagramType.Assembly.GetType(configuration.ModelTypeName());
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
            var modelNode = modelListView.GetNode(typeof(GridView).Name);

            (modelNode is IModelPredifinedMapExtension).ShouldBeTrue();
            
        }

        [Theory]
        [InlineData(Platform.Web,PredifinedMap.ASPxHyperLink)]
        [InlineData(Platform.Win,PredifinedMap.RichEditControl)]
        [InlineData(Platform.Win,PredifinedMap.RepositoryItem)]
        internal void Extend_Existing_ViewItemMap(Platform platform,PredifinedMap predifinedMap){
            var mapPropertyName=predifinedMap.IsRepositoryItem()?ViewItemService.RepositoryItemsMapName:ViewItemService.PropertyEditorControlMapName;
            InitializeMapperService(nameof(Extend_Existing_ViewItemMap),platform);
            var module = new []{predifinedMap}.Extend();
            
            module.ApplicationModulesManager
                .FirstAsync()
                .SelectMany(_ => _.ExtendMap(predifinedMap))
                .Subscribe(_ => {
                    _.extenders.Add(_.targetInterface,typeof(IModelPredifinedMapExtension));
                });
            var application = DefaultModelMapperModule(platform,module).Application;
            var nodeByPath = application.Model.GetNodeByPath(MMDetailViewTestItemNodePath);
            nodeByPath.ShouldNotBeNull();
            
            var listNode = nodeByPath.GetNode(mapPropertyName);
            listNode.ShouldNotBeNull();
            var baseType = listNode.ModelListItemType();
            var modelType = baseType.ToTypeInfo().Descendants.First().Type;

            (listNode.AddNode(modelType) is IModelPredifinedMapExtension).ShouldBeTrue();
            
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
            modelModelMappers.First().Id().ShouldBe(typeToMap.Name);
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
            var mapName = typeToMap.Name;
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
                var propertyExistsCriteria = VisibilityCriteriaLeftOperand.PropertyExists.GetVisibilityCriteria("EditorType","Parent");
                visibilityCriteria = visibilityCriteriaLeftOperand.GetVisibilityCriteria(rightOperand,path);
                visibilityCriteria = $"{propertyExistsCriteria} and {visibilityCriteria}";
            }

            var platform = Platform.Win;
            InitializeMapperService($"{nameof(Container_Visibility)}{visibilityCriteria.CleanCodeName()}{platform}");
            var typeToMap = typeof(TestModelMapper);

            var module = typeToMap.Extend(configuration => {
                configuration.VisibilityCriteria = visibilityCriteria;
                configuration.TargetInterfaceTypes.Add(typeof(IModelListView));
                if (leftOperand is VisibilityCriteriaLeftOperand ){
                    configuration.TargetInterfaceTypes.Add(typeof(IModelOptions));
                }
            });

            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var modelMapName = typeToMap.Name;
            modelListView.IsPropertyVisible(modelMapName).ShouldBe(visibility);
            if (leftOperand is VisibilityCriteriaLeftOperand){
                application.Model.Options.IsPropertyVisible(modelMapName).ShouldBe(false);
            }

        }



    }
}
