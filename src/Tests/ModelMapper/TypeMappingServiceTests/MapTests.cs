using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.DashboardWeb;
using DevExpress.DashboardWin;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.HtmlPropertyEditor.Web;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.Scheduler.Web;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Web;
using DevExpress.Web.ASPxHtmlEditor;
using DevExpress.Web.ASPxScheduler;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Layout;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraRichEdit;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using EnumsNET;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [Collection(nameof(ModelMapperModule))]
    public class MapTests:ModelMapperBaseTest{
        [Fact]
        public async Task Map_RW_StringValueType_Public_Properties(){
            InitializeMapperService(nameof(Map_RW_StringValueType_Public_Properties));
            var typeToMap = typeof(StringValueTypeProperties);
            var propertiesToMap = typeToMap.Properties().Where(info => info.CanRead&&info.CanWrite).ToArray();

            var modelType = await typeToMap.MapToModel().ModelInterfaces();


            var modelTypeProperties = ModelTypeProperties(modelType);
            foreach (var propertyInfo in propertiesToMap){
                var modelProperty = modelTypeProperties.FirstOrDefault(info => info.Name==propertyInfo.Name);
                modelProperty.ShouldNotBeNull(propertyInfo.Name);
                var modelPropertyPropertyType = modelProperty.PropertyType;
                var propertyInfoPropertyType = propertyInfo.PropertyType;
                if (!propertyInfoPropertyType.IsGenericType){
                    if (propertyInfoPropertyType.IsValueType){
                        modelPropertyPropertyType.GetGenericTypeDefinition().ShouldBe(typeof(Nullable<>));
                        modelPropertyPropertyType.GetGenericArguments().First().ShouldBe(propertyInfoPropertyType);
                    }
                    else{
                        modelPropertyPropertyType.ShouldBe(propertyInfoPropertyType);
                    }
                }
                else{
                    modelPropertyPropertyType.ShouldBe(propertyInfoPropertyType);
                }

            }
            modelTypeProperties.Length.ShouldBe(propertiesToMap.Length);
        }

        [Theory]
        [InlineData(typeof(CollectionsType), new[] {
            nameof(CollectionsType.TestModelMappersList), nameof(CollectionsType.TestModelMappersArray),
            nameof(CollectionsType.ValueTypeArray)
        })]
        public async Task Include_Collections(Type typeToMap,string[] collectionNames){
            InitializeMapperService($"{nameof(Include_Collections)}");
            
            var modelType = await typeToMap.MapToModel().ModelInterfaces();
            var modelTypeProperties = ModelTypeProperties(modelType);

            foreach (var collectionName in collectionNames){
                modelTypeProperties.FirstOrDefault(info => info.Name==collectionName).ShouldNotBeNull();    
            }

        }


        [Fact]
        public async Task Map_All_ReferenceType_Public_Properties(){
            
            InitializeMapperService(nameof(Map_All_ReferenceType_Public_Properties));
            var typeToMap = typeof(ReferenceTypeProperties);
            var propertiesToMap = typeToMap.Properties().ToArray();

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            foreach (var propertyInfo in propertiesToMap){
                var modelProperty = modelTypeProperties.FirstOrDefault(info => info.Name==propertyInfo.Name);
                modelProperty.ShouldNotBeNull(propertyInfo.Name);
                modelProperty.PropertyType.Name.ShouldBe($"{propertyInfo.PropertyType.ModelTypeName(typeToMap)}");
            }

            modelTypeProperties.Length.ShouldBe(propertiesToMap.Length);
        }

        [Fact]
        public async Task Map_Nested_type_properties(){
            InitializeMapperService(nameof(Map_Nested_type_properties));
            var typeToMap = typeof(NestedTypeProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(1);
        }

        [Fact]
        public async Task Map_Multiple_Objects_from_the_same_subscription_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_from_the_same_subscription_In_the_same_assembly));

            var mappedTypes = new[]{typeToMap1, typeToMap2}.MapToModel().ModelInterfaces();

            var mappedType1 = await mappedTypes.Take(1);
            mappedType1.Name.ShouldBe(typeToMap1.ModelTypeName());
            var mappedType2 = await mappedTypes.Take(2);
            mappedType2.Name.ShouldBe(typeToMap2.ModelTypeName());
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Fact]
        public void Map_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService(nameof(Map_Multiple_Objects_with_common_types));

            var mappedTypes = new List<Type>(new[]{typeToMap1, typeToMap2}.MapToModel().ModelInterfaces().ToEnumerable().ToArray());

            var mappedType1 = mappedTypes[0];
            var typesToMap = new[]{typeToMap1,typeToMap2}.Select(type => type.ModelTypeName());
            typesToMap.ShouldContain(mappedType1.Name);
            var appearenceCell = mappedType1.Properties().First(_ => _.Name==nameof(TestModelMapperCommonType1.AppearanceCell));
            appearenceCell.ShouldNotBeNull();
            appearenceCell.GetType().Properties("TextOptions").ShouldNotBeNull();
            mappedTypes.Remove(mappedType1);

            var mappedType2 = mappedTypes[0];
            mappedType2.Name.ShouldBe(typeToMap2.ModelTypeName());
            appearenceCell = mappedType1.Properties().First(_ => _.Name==nameof(TestModelMapperCommonType2.AppearanceCell));
            appearenceCell.ShouldNotBeNull();
            appearenceCell.GetType().Properties("TextOptions").ShouldNotBeNull();
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Fact]
        public async Task Map_Multiple_Objects_from_the_different_subscription_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_from_the_different_subscription_In_the_same_assembly));

            await new[]{typeToMap1}.MapToModel();
            await new[]{typeToMap2}.MapToModel();

            typeof(TypeMappingService).Method("Start",Flags.StaticInstanceAnyVisibility).Call(null);
            var mappedType1 = await TypeMappingService.MappedTypes.Take(1);
            mappedType1.Name.ShouldBe(typeToMap1.ModelTypeName());
            var mappedType2 = await TypeMappingService.MappedTypes.Take(2);
            mappedType2.Name.ShouldBe(typeToMap2.ModelTypeName());
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Theory]

        [InlineData(PredefinedMap.GridColumn,new[]{typeof(GridColumn),typeof(GridListEditor)},Platform.Win,new[]{nameof(GridColumn.Summary)})]
        [InlineData(PredefinedMap.GridView,new[]{typeof(GridView),typeof(GridListEditor)},Platform.Win,new[]{nameof(GridView.FormatRules)})]
        [InlineData(PredefinedMap.PivotGridControl,new[]{typeof(PivotGridControl),typeof(PivotGridListEditor)},Platform.Win,new[]{nameof(PivotGridControl.FormatRules)})]
        [InlineData(PredefinedMap.PivotGridField,new[]{typeof(PivotGridField),typeof(PivotGridListEditor)},Platform.Win,new[]{nameof(PivotGridField.CustomTotals)})]
        [InlineData(PredefinedMap.LayoutViewColumn,new[]{typeof(LayoutViewColumn),typeof(GridListEditor)},Platform.Win,new[]{nameof(LayoutViewColumn.Summary)})]
        [InlineData(PredefinedMap.LayoutView,new[]{typeof(LayoutView),typeof(GridListEditor)},Platform.Win,new[]{nameof(LayoutView.FormatRules)})]
        [InlineData(PredefinedMap.BandedGridColumn,new[]{typeof(BandedGridColumn),typeof(GridListEditor)},Platform.Win,new[]{nameof(BandedGridColumn.Summary)})]
        [InlineData(PredefinedMap.AdvBandedGridView,new[]{typeof(AdvBandedGridView),typeof(GridListEditor)},Platform.Win,new[]{nameof(AdvBandedGridView.FormatRules)})]
        [InlineData(PredefinedMap.ASPxGridView,new[]{typeof(ASPxGridView),typeof(ASPxGridListEditor)},Platform.Web,new[]{nameof(ASPxGridView.Columns)})]
        [InlineData(PredefinedMap.GridViewColumn,new[]{typeof(GridViewColumn),typeof(ASPxGridListEditor)},Platform.Web,new[]{nameof(GridViewColumn.Columns)})]
        [InlineData(PredefinedMap.ASPxHtmlEditor,new[]{typeof(ASPxHtmlEditor),typeof(ASPxHtmlPropertyEditor)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.TreeList,new[]{typeof(TreeList),typeof(TreeListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.TreeListColumn,new[]{typeof(TreeListColumn),typeof(TreeListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.SchedulerControl,new[]{typeof(SchedulerControl),typeof(SchedulerListEditor)},Platform.Win,new[]{nameof(SchedulerControl.DataBindings)})]
        [InlineData(PredefinedMap.ASPxScheduler,new[]{typeof(ASPxScheduler),typeof(ASPxSchedulerListEditor)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.XafLayoutControl,new[]{typeof(XafLayoutControl)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.SplitContainerControl,new[]{typeof(SplitContainerControl)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.DashboardDesigner,new[]{typeof(DashboardDesigner)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ASPxPopupControl,new[]{typeof(ASPxPopupControl)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.DashboardViewer,new[]{typeof(DashboardViewer)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ASPxDashboard,new[]{typeof(ASPxDashboard)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxDateEdit,new[]{typeof(ASPxDateEdit)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxHyperLink,new[]{typeof(ASPxHyperLink)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxLookupDropDownEdit,new[]{typeof(ASPxLookupDropDownEdit)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxLookupFindEdit,new[]{typeof(ASPxLookupFindEdit)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxSpinEdit,new[]{typeof(ASPxSpinEdit)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxTokenBox,new[]{typeof(ASPxTokenBox)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.ASPxComboBox,new[]{typeof(ASPxComboBox)},Platform.Web,new string[0])]
        [InlineData(PredefinedMap.LabelControl,new[]{typeof(LabelControl)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.RichEditControl,new[]{typeof(RichEditControl)},Platform.Win,new string[0])]

        internal async Task Map_Predefined_Configurations(PredefinedMap predefinedMap, Type[] assembliesToLoad,Platform platform, string[] collectionNames){
            
            InitializeMapperService($"{nameof(Map_Predefined_Configurations)}{predefinedMap}",platform);
            assembliesToLoad.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();

            var modelType = await predefinedMap.MapToModel().ModelInterfaces().FirstAsync();
            var propertyInfos = modelType.GetProperties();

            AssertPredefinedConfigurationsMap(predefinedMap, collectionNames, modelType, propertyInfos);
            AssertBandedGridColumn(predefinedMap, propertyInfos);
            
            AssertSchedulerControl(predefinedMap, propertyInfos);

        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private void AssertSchedulerControl(PredefinedMap predefinedMap, IList<PropertyInfo> propertyInfos){
            if (predefinedMap == PredefinedMap.SchedulerControl){
                var storageInfo = propertyInfos.FirstOrDefault(info => nameof(SchedulerControl.Storage) == info.Name);
                storageInfo.ShouldNotBeNull();
                var propertyInfo = storageInfo.PropertyType.Property(nameof(SchedulerStorage.Appointments)).PropertyType.Property(nameof(AppointmentStorage.Labels));
                propertyInfo.ShouldNotBeNull();
                var menusPropertyInfo = propertyInfos.FirstOrDefault(info => info.Name == SchedulerControlService.PopupMenusMoelPropertyName);
                menusPropertyInfo.PropertyType.ModelListType().ShouldNotBeNull();
            }
        }

        [Theory]
        [InlineData(Platform.Win)]
        internal async Task Map_PredefinedMap_RepositoryItems(Platform platform){
            var predefinedMaps = Enums.GetValues<PredefinedMap>().Where(map => map.IsRepositoryItem())
                .Where(map => map.Attribute<MapPlatformAttribute>().Platform == platform.ToString());
//                .Where(map => map==PredefinedMap.RepositoryItem);

            await Map_PredefinedMap_ViewItems(platform, predefinedMaps, typeof(RepositoryItemBaseMap).ModelTypeName(), ViewItemService.RepositoryItemsMapName,true);
        }

        private async Task Map_PredefinedMap_ViewItems(Platform platform, IEnumerable<PredefinedMap> predefinedMaps,string mapTypeName, string mapPropertyName,bool checkDescription=false){
            foreach (var predefinedMap in predefinedMaps){
                try{
                    InitializeMapperService($"{nameof(Map_PredefinedMap_ViewItems)}{predefinedMap}", platform);

                    var replay = predefinedMap.MapToModel().ModelInterfaces().Replay();
                    replay.Connect();
                    await replay;
                    var modelTypes = replay.ToEnumerable().ToArray();

                    var propertyInfos = modelTypes.Last().GetProperties();
                    if (checkDescription){
                        var descriptionAttribute = propertyInfos.Select(info => info.Attribute<DescriptionAttribute>())
                            .Where(attribute => attribute != null)
                            .FirstOrDefault(attribute => attribute.Description.Contains(" "));
                        descriptionAttribute.ShouldNotBeNull();
                    }

                    foreach (var modelType in modelTypes){
                        modelType.Property(TypeMappingService.ModelMappersNodeName).ShouldBeNull();
                    }

                    var modelMapperContainerType = modelTypes.First().ModelMapperContainerTypes().Single();
                    var propertyInfo = modelMapperContainerType.Property(mapPropertyName);
                    propertyInfo.ShouldNotBeNull();
                    var listType = propertyInfo.PropertyType.ModelListType();
//                    var listType = propertyInfo.PropertyType.GetInterfaces().FirstOrDefault(type =>
//                        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IModelList<>));
                    listType.ShouldNotBeNull();
                    
                    var baseType = modelTypes.First().Assembly.GetType(mapTypeName);
                    propertyInfo.PropertyType.ModelListItemType().ShouldBe(baseType);
                    var realType = modelTypes.First().Assembly.GetTypes()
                        .FirstOrDefault(type => type.Name == predefinedMap.ModelTypeName());
                    realType.ShouldNotBeNull();
                    realType.GetInterfaces().ShouldContain(baseType);
                    realType.Property(TypeMappingService.ModelMappersNodeName).ShouldBeNull();

                    Dispose();
                }
                catch (Exception e){
                    throw new Exception(predefinedMap.ToString(), e);
                }
            }
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task Map_PredefinedMap_PropertyEditor_Controls(Platform platform){
            var predefinedMaps = Enums.GetValues<PredefinedMap>().Where(map => map.IsPropertyEditor())
                .Where(map => map.Attribute<MapPlatformAttribute>().Platform==platform.ToString());
            
            await Map_PredefinedMap_ViewItems(platform, predefinedMaps, typeof(PropertyEditorControlMap).ModelTypeName(), ViewItemService.PropertyEditorControlMapName);
        }

        [Theory]
        [InlineData(PredefinedMap.ChartControl,new[]{typeof(ChartControl),typeof(ChartListEditor)},Platform.Win,new[]{nameof(ChartControl.Series),"Diagrams"})]
        [InlineData(PredefinedMap.ChartControlDiagram3D,new[]{typeof(Diagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlSimpleDiagram3D,new[]{typeof(SimpleDiagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlFunnelDiagram3D,new[]{typeof(FunnelDiagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlGanttDiagram,new[]{typeof(GanttDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlPolarDiagram,new[]{typeof(PolarDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlRadarDiagram,new[]{typeof(RadarDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlSwiftPlotDiagram,new[]{typeof(SwiftPlotDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlXYDiagram,new[]{typeof(XYDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlXYDiagram2D,new[]{typeof(XYDiagram2D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredefinedMap.ChartControlXYDiagram3D,new[]{typeof(XYDiagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        internal async Task Map_Predefined_ChartControl_Configurations(PredefinedMap configuration,Type[] assembliesToLoad,Platform platform,string[] collectionNames){
            InitializeMapperService($"{nameof(Map_Predefined_ChartControl_Configurations)}{configuration}",platform);
            assembliesToLoad.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();

            var modelType = await configuration.MapToModel().ModelInterfaces();
            var propertyInfos = modelType.GetProperties();
            AssertPredefinedConfigurationsMap(configuration, collectionNames, modelType, propertyInfos);
            if (configuration == PredefinedMap.ChartControl){
                propertyInfos.FirstOrDefault(info => nameof(ChartControl.Diagram) == info.Name).ShouldBeNull();
                var propertyInfo = propertyInfos.FirstOrDefault(info => info.Name == $"{nameof(ChartControl.Diagram)}s");
                propertyInfo.ShouldNotBeNull();
                var type = modelType.Assembly.GetType(typeof(Diagram).ModelTypeName(typeof(ChartControl)));
                propertyInfo.PropertyType.GetInterfaces().ShouldContain(typeof(IModelList<>).MakeGenericType(type));
            }
        }

        private void AssertPredefinedConfigurationsMap(PredefinedMap predefinedMap, string[] collectionNames,Type modelType, PropertyInfo[] propertyInfos){
            var modelTypeName = predefinedMap.ModelTypeName();
            modelType.Name.ShouldBe(modelTypeName);

            propertyInfos.Length.ShouldBeGreaterThan(15);
            if (new[]{PredefinedMap.ASPxLookupDropDownEdit,PredefinedMap.ASPxLookupFindEdit, }.All(map => map!=predefinedMap)){
                var descriptionAttribute = propertyInfos.Select(info => info.Attribute<DescriptionAttribute>())
                    .FirstOrDefault(attribute => attribute != null && attribute.Description.Contains(" "));
                descriptionAttribute.ShouldNotBeNull();
                foreach (var collectionName in collectionNames){
                    propertyInfos.FirstOrDefault(info => info.Name == collectionName).ShouldNotBeNull();
                }
            }
        }

        private static void AssertBandedGridColumn(PredefinedMap configuration, IList<PropertyInfo> propertyInfos){
            if (configuration == PredefinedMap.BandedGridColumn){
                var propertyInfo = propertyInfos.FirstOrDefault(info => info.Name == nameof(BandedGridColumn.ColVIndex));
                propertyInfo.ShouldNotBeNull();
                propertyInfo.Attribute<BrowsableAttribute>().ShouldBeNull();
                propertyInfo = propertyInfos.FirstOrDefault(info => info.Name == nameof(BandedGridColumn.RowIndex));
                propertyInfo.ShouldNotBeNull();
                propertyInfo.Attribute<BrowsableAttribute>().ShouldBeNull();
            }
        }


        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Map_All_PredefinedConfigurations(Platform platform){
            Assembly.LoadFile(typeof(ChartControl).Assembly.Location);
            InitializeMapperService($"{nameof(Map_All_PredefinedConfigurations)}",platform);
            var values = Enums.GetValues<PredefinedMap>()
                .Where(map =>map.GetAttributes().OfType<MapPlatformAttribute>().Any(_ => _.Platform == platform.ToString()))
                .ToArray();
            var modelInterfaces = values.MapToModel().ModelInterfaces().Replay();
            modelInterfaces.Connect();

            var types = modelInterfaces.ToEnumerable().ToArray();
            types.Length.ShouldBeGreaterThan(0);
            types.Length.ShouldBe(values.Length);
            foreach (var map in values){
                var modelTypeName = map.ModelTypeName();
                types.FirstOrDefault(_ => _.Name == modelTypeName).ShouldNotBeNull();
            }
        }

        [Fact]
        internal void Map_PredefinedConfigurations_Combination(){
            InitializeMapperService($"{nameof(Map_All_PredefinedConfigurations)}",Platform.Win);

            var modelInterfaces = new[]{PredefinedMap.GridView,PredefinedMap.GridColumn}.MapToModel().ModelInterfaces().Replay();
            modelInterfaces.Connect();

            var types = modelInterfaces.ToEnumerable().ToArray();
            types.Length.ShouldBe(2);
            var modelNames = new[]{PredefinedMap.GridView,PredefinedMap.GridColumn }.Select(map => map.ModelTypeName()).ToArray();
            modelNames.ShouldContain(types.First().Name);
            modelNames.ShouldContain(types.Last().Name);
            
        }
    }

    
}
