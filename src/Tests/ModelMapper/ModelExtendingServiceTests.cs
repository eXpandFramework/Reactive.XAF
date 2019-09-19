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
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
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
            using (var application = DefaultModelMapperModule(nameof(ExtendModel_Any_Type), platform, module).Application){
                AssertExtendedListViewModel(typeToMap, application, MMListViewNodePath);
            }
        }

        [Fact]
        public void Get_PredefinedModelNode(){
            InitializeMapperService($"{nameof(Customize_PredifienedMaps_TargetInterface)}",Platform.Win);

            using (var module = PredefinedMap.GridView.Extend()){
                using (var application = DefaultModelMapperModule(nameof(Get_PredefinedModelNode), Platform.Win, module).Application){
                    application.Model.Views.OfType<IModelListView>().First().GetNode(PredefinedMap.GridView).ShouldNotBeNull();
                }
            }
        }

        [Fact]
        public void Get_PredefinedViewItemMergedModelNode(){
            InitializeMapperService($"{nameof(Customize_PredifienedMaps_TargetInterface)}",Platform.Win);

            using (var module = new[]{PredefinedMap.RepositoryItem, PredefinedMap.RepositoryFieldPicker, PredefinedMap.RepositoryItemBlobBaseEdit}.Extend()){
                using (var application = DefaultModelMapperModule(nameof(Get_PredefinedViewItemMergedModelNode), Platform.Win, module).Application){
                    var modelColumn = application.Model.GetNodeByPath(MMListViewTestItemNodePath);
                    var repositoryItemModel = modelColumn.AddRepositoryItemNode(PredefinedMap.RepositoryItem);
                    repositoryItemModel.SetValue("Name","Base");
                    repositoryItemModel.SetValue("AccessibleName","AccessibleNameBase");
                    var repositoryFieldPickerModel = modelColumn.AddRepositoryItemNode(PredefinedMap.RepositoryFieldPicker);
                    repositoryFieldPickerModel.SetValue("Name","Derivved");
                    repositoryFieldPickerModel.SetValue("AccessibleName","AccessibleName");
                    var repositoryItemBlobBaseEdit = modelColumn.AddRepositoryItemNode(PredefinedMap.RepositoryItemBlobBaseEdit);
                    repositoryItemBlobBaseEdit.SetValue("Name","NotMerged");

                    var finalNode = modelColumn.GetRepositoryItemNode(PredefinedMap.RepositoryFieldPicker);
                    finalNode.GetValue<string>("Name").ShouldBe("Derivved");
                    finalNode.GetValue<string>("AccessibleName").ShouldBe("AccessibleName");

                    modelColumn.GetRepositoryItemNode(PredefinedMap.RepositoryItemBlobBaseEdit).GetValue<string>("Name").ShouldBe("NotMerged");
                }
            }
        }

        [Fact]
        internal void Customize_PredifienedMaps_TargetInterface(){
            InitializeMapperService($"{nameof(Customize_PredifienedMaps_TargetInterface)}",Platform.Win);

            using (var module = PredefinedMap.DashboardDesigner.Extend(null, configuration => {
                configuration.TargetInterfaceTypes.Clear();
                configuration.TargetInterfaceTypes.Add(typeof(IModelOptions));
            })){
                using (var application = DefaultModelMapperModule(nameof(Customize_PredifienedMaps_TargetInterface), Platform.Win, module).Application){
                    application.Model.Options.GetNode(PredefinedMap.DashboardDesigner).ShouldNotBeNull();
                }
            }

            
        }

        [Theory]
        [InlineData(PredefinedMap.GridColumn, typeof(GridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredefinedMap.GridView, typeof(GridView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredefinedMap.SchedulerControl, typeof(SchedulerControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredefinedMap.PivotGridControl, typeof(PivotGridControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredefinedMap.ChartControl, typeof(ChartControl),Platform.Win,MMListViewNodePath)]
        [InlineData(PredefinedMap.PivotGridField, typeof(PivotGridField),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredefinedMap.LayoutViewColumn, typeof(LayoutViewColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredefinedMap.LayoutView, typeof(LayoutView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredefinedMap.BandedGridColumn, typeof(BandedGridColumn),Platform.Win,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredefinedMap.AdvBandedGridView, typeof(AdvBandedGridView),Platform.Win,MMListViewNodePath)]
        [InlineData(PredefinedMap.GridViewColumn, typeof(GridViewColumn),Platform.Web,MMListViewNodePath+"/Columns/Test")]
        [InlineData(PredefinedMap.ASPxGridView, typeof(ASPxGridView),Platform.Web,MMListViewNodePath)]
        [InlineData(PredefinedMap.TreeList, typeof(TreeList),Platform.Win,MMListViewNodePath+",NavigationItems")]
        [InlineData(PredefinedMap.TreeListColumn, typeof(TreeListColumn),Platform.Win,MMListViewTestItemNodePath)]
        [InlineData(PredefinedMap.ASPxScheduler, typeof(ASPxScheduler),Platform.Web,MMListViewNodePath)]
        [InlineData(PredefinedMap.XafLayoutControl, typeof(XafLayoutControl),Platform.Win,MMDetailViewNodePath)]
        [InlineData(PredefinedMap.SplitContainerControl, typeof(SplitContainerControl),Platform.Win,MMListViewNodePath+"/SplitLayout")]
        [InlineData(PredefinedMap.DashboardDesigner, typeof(DashboardDesigner),Platform.Win,MMDetailViewTestItemNodePath)]
        [InlineData(PredefinedMap.ASPxPopupControl, typeof(ASPxPopupControl),Platform.Web,MMListViewNodePath+","+MMDetailViewNodePath)]
        internal void ExtendModel_Predefined_Type(PredefinedMap configuration,Type typeToMap,Platform platform,string nodePath){
            Assembly.LoadFile(typeToMap.Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_Predefined_Type)}{configuration}{platform}",platform);

            using (var module = configuration.Extend()){
                using (var application = DefaultModelMapperModule(nameof(ExtendModel_Predefined_Type), platform, module).Application){
                    AssertExtendedListViewModel(typeToMap, application,nodePath);
                }
            }

            
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
        internal void Extend_PredefinedRepositoryItems(){
            var predefinedMaps = Enums.GetValues<PredefinedMap>()
                .Where(map => map.IsRepositoryItem());
            Extend_Predifiened_ViewItems(predefinedMaps,Platform.Win, ViewItemService.RepositoryItemsMapName,true);
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Extend_Predefined_PropertyEditorControls(Platform platform){
            var predefinedMaps = Enums.GetValues<PredefinedMap>()
                .Where(map => map.IsPropertyEditor()&&map.Attribute<MapPlatformAttribute>().Platform==platform.ToString());
            new[]{typeof(ASPxDashboard)}.ToObservable().Subscribe();
            Extend_Predifiened_ViewItems(predefinedMaps,platform, ViewItemService.PropertyEditorControlMapName);
        }

        private void Extend_Predifiened_ViewItems(IEnumerable<PredefinedMap> predefinedMaps, Platform platform,string mapPropertyName,bool checkListViewColumns=false){
            foreach (var predefinedMap in predefinedMaps){
                try{
                    InitializeMapperService($"{nameof(Extend_Predifiened_ViewItems)}{predefinedMap}", platform);
                    using (var module = predefinedMap.Extend()){
                        var connectableObservable = TypeMappingService.MappedTypes.Replay();
                        connectableObservable.Connect();
                        using (var application = DefaultModelMapperModule($"{nameof(Extend_Predifiened_ViewItems)}-{predefinedMap}", platform, module).Application){
                            var typeToMap = predefinedMap.TypeToMap();
                    
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
                            var modelMapper = defaultContext.GetNode(predefinedMap.DisplayName());
                            modelMapper.ShouldNotBeNull();
                            application.Dispose();
                        }

                        Dispose();
                    }

                    
                }
                catch (Exception e){
                    throw new Exception(predefinedMap.ToString(), e);
                }
            }
        }

        [Theory]
        [InlineData(PredefinedMap.ChartControlRadarDiagram, typeof(RadarDiagram),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlPolarDiagram, typeof(PolarDiagram),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlXYDiagram2D, typeof(XYDiagram2D),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlXYDiagram, typeof(XYDiagram),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlSwiftPlotDiagram, typeof(SwiftPlotDiagram),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlGanttDiagram, typeof(GanttDiagram),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlFunnelDiagram3D, typeof(FunnelDiagram3D),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlDiagram3D, typeof(Diagram3D),Platform.Win)]
        [InlineData(PredefinedMap.ChartControlSimpleDiagram3D, typeof(SimpleDiagram3D),Platform.Win)]
        internal void ExtendModel_PredefinedChartDiagram(PredefinedMap configuration,Type typeToMap,Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_PredefinedChartDiagram)}{configuration}{platform}",platform);

            using (var module = PredefinedMap.ChartControl.Extend()){
                configuration.Extend(module);
                using (var application = DefaultModelMapperModule(nameof(ExtendModel_PredefinedChartDiagram), platform, module).Application){
                    var modelListView = application.Model.Views.OfType<IModelListView>().First();
                    var modelNode = modelListView.GetNode(PredefinedMap.ChartControl);
                    modelNode= modelNode.GetNode("Diagrams");

                    var diagramType = modelNode.ModelListItemType();
                    var targetType = diagramType.Assembly.GetType(configuration.ModelTypeName());
                    diagramType.IsAssignableFrom(targetType).ShouldBeTrue();
                }
            }
        }

        [Fact]
        internal void ExtendModel_All_PredefinedChartDiagram(){
            Assembly.LoadFile(typeof(ChartControl).Assembly.Location);
            Assembly.LoadFile(typeof(Diagram).Assembly.Location);
            InitializeMapperService($"{nameof(ExtendModel_All_PredefinedChartDiagram)}",Platform.Win);

            using (var module = PredefinedMap.ChartControl.Extend()){
                var diagrams = Enums.GetMembers<PredefinedMap>().Where(member =>
                    member.Name.StartsWith(PredefinedMap.ChartControl.ToString()) &&
                    member.Value != PredefinedMap.ChartControl&&member.Value != PredefinedMap.ChartControlDiagram).Select(member => member.Value).ToArray();
                diagrams.Extend(module);
                using (DefaultModelMapperModule(nameof(ExtendModel_All_PredefinedChartDiagram), Platform.Win, module).Application){
                }
            }
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void ExtendModel_All_Predefined_Maps(Platform platform){
            InitializeMapperService($"{nameof(ExtendModel_All_Predefined_Maps)}",platform);
            var values = Enums.GetValues<PredefinedMap>()
                .Where(map =>map.GetAttributes().OfType<MapPlatformAttribute>().Any(_ => _.Platform == platform.ToString()))
                .ToArray();

            using (var module = values.ToArray().Extend()){
                using (DefaultModelMapperModule(nameof(ExtendModel_All_Predefined_Maps), platform, module).Application){

                }
            }
        }

        [Fact]
        public void Extend_Existing_PredefinedMap(){
            InitializeMapperService(nameof(Extend_Existing_PredefinedMap),Platform.Win);
            using (var module = new[]{PredefinedMap.PivotGridControl, PredefinedMap.GridView}.Extend()){
                module.ApplicationModulesManager
                    .FirstAsync()
                    .SelectMany(_ => _.manager.ExtendMap(PredefinedMap.GridView))
                    .Subscribe(_ => {
                        _.extenders.Add(_.targetInterface,typeof(IModelPredefinedMapExtension));
                    });
                using (var application = DefaultModelMapperModule(nameof(Extend_Existing_PredefinedMap), Platform.Win, module).Application){
                    var modelListView = application.Model.Views.OfType<IModelListView>().First();
                    var modelNode = modelListView.GetNode(typeof(GridView).Name);

                    (modelNode is IModelPredefinedMapExtension).ShouldBeTrue();
                }
            }
        }

        [Theory]
        [InlineData(Platform.Web,PredefinedMap.ASPxHyperLink)]
        [InlineData(Platform.Win,PredefinedMap.RichEditControl)]
        [InlineData(Platform.Win,PredefinedMap.RepositoryItem)]
        internal void Extend_Existing_ViewItemMap(Platform platform,PredefinedMap predefinedMap){
            var mapPropertyName=predefinedMap.IsRepositoryItem()?ViewItemService.RepositoryItemsMapName:ViewItemService.PropertyEditorControlMapName;
            InitializeMapperService(nameof(Extend_Existing_ViewItemMap),platform);
            using (var module = new[]{predefinedMap}.Extend()){
                module.ApplicationModulesManager
                    .FirstAsync()
                    .SelectMany(_ => _.manager.ExtendMap(predefinedMap))
                    .Subscribe(_ => {
                        _.extenders.Add(_.targetInterface,typeof(IModelPredefinedMapExtension));
                    });
                using (var application = DefaultModelMapperModule(nameof(Extend_Existing_ViewItemMap), platform, module).Application){
                    var nodeByPath = application.Model.GetNodeByPath(MMDetailViewTestItemNodePath);
                    nodeByPath.ShouldNotBeNull();
            
                    var listNode = nodeByPath.GetNode(mapPropertyName);
                    listNode.ShouldNotBeNull();
                    var baseType = listNode.ModelListItemType();
                    var modelType = baseType.ToTypeInfo().Descendants.First().Type;

                    (listNode.AddNode(modelType) is IModelPredefinedMapExtension).ShouldBeTrue();
                }
            }
        }

        [Fact]
        public void Extend_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService(nameof(Extend_Multiple_Objects_with_common_types));

            using (var module = new ModelMapperTestModule()){
                typeToMap1.Extend<IModelListView>(module);
                typeToMap2.Extend<IModelColumn>(module);

                using (var application = DefaultModelMapperModule(nameof(Extend_Multiple_Objects_with_common_types), Platform.Win, module).Application){
                    var appearanceCell = application.Model.GetNodeByPath($@"{MMListViewNodePath}/Columns/Test/{nameof(TestModelMapperCommonType2)}/{nameof(TestModelMapperCommonType2.AppearanceCell)}");
                    appearanceCell.ShouldNotBeNull();
                    appearanceCell.GetNodeByPath($"{nameof(AppearanceObjectEx.TextOptions)}");
                }
            }
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void ModelMapperContexts(Platform platform){
            InitializeMapperService($"{nameof(ModelMapperContexts)}{platform}");
            var typeToMap = typeof(TestModelMapper);

            using (var module = typeToMap.Extend<IModelListView>()){
                using (var application = DefaultModelMapperModule(nameof(ModelMapperContexts), platform, module).Application){
                    var modelModelMappers = ((IModelApplicationModelMapper) application.Model).ModelMapper.MapperContexts.First();
                    modelModelMappers.Id().ShouldBe(ModelMapperContextNodeGenerator.Default);
                    modelModelMappers.First().Id().ShouldBe(typeToMap.Name);
                }
            }
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Container_ModelMapperContexts(Platform platform){
            InitializeMapperService($"{nameof(Container_ModelMapperContexts)}{platform}");
            var typeToMap = typeof(TestModelMapper);

            using (var module = typeToMap.Extend<IModelListView>()){
                using (var application = DefaultModelMapperModule(nameof(Container_ModelMapperContexts), platform, module).Application){
                    var modelListView = application.Model.Views.OfType<IModelListView>().First();
                    var mapName = typeToMap.Name;
                    var modelMappersNode = modelListView.GetNode(mapName).GetNode(TypeMappingService.ModelMappersNodeName);
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
            }
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

            using (var module = typeToMap.Extend(configuration => {
                configuration.VisibilityCriteria = visibilityCriteria;
                configuration.TargetInterfaceTypes.Add(typeof(IModelListView));
                if (leftOperand is VisibilityCriteriaLeftOperand){
                    configuration.TargetInterfaceTypes.Add(typeof(IModelOptions));
                }
            })){
                using (var application = DefaultModelMapperModule(nameof(Container_Visibility), platform, module).Application){
                    var modelListView = application.Model.Views.OfType<IModelListView>().First();
                    var modelMapName = typeToMap.Name;
                    modelListView.IsPropertyVisible(modelMapName).ShouldBe(visibility);
                    if (leftOperand is VisibilityCriteriaLeftOperand){
                        application.Model.Options.IsPropertyVisible(modelMapName).ShouldBe(false);
                    }
                }
            }
        }
    }
}
