using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using DevExpress.XtraLayout;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using EnumsNET;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predifined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xunit;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{

    public partial class ObjectMappingServiceTests{
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
                modelProperty.PropertyType.Name.ShouldBe($"{propertyInfo.PropertyType.ModelMapName(typeToMap)}");
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
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var mappedType2 = await mappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Fact]
        public async Task Map_Multiple_Objects_with_common_types(){
            var typeToMap1 = typeof(TestModelMapperCommonType1);
            var typeToMap2 = typeof(TestModelMapperCommonType2);
            InitializeMapperService(nameof(Map_Multiple_Objects_with_common_types));

            var mappedTypes = new[]{typeToMap1, typeToMap2}.MapToModel().ModelInterfaces();

            var mappedType1 = await mappedTypes.Take(1);
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var appearenceCell = mappedType1.Properties().First(_ => _.Name==nameof(TestModelMapperCommonType1.AppearanceCell));
            appearenceCell.ShouldNotBeNull();
            appearenceCell.GetType().Properties("TextOptions").ShouldNotBeNull();
            var mappedType2 = await mappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
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
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.Name}");
            var mappedType2 = await TypeMappingService.MappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.Name}");
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Theory]
//        [InlineData(PredifinedMap.SchedulerControl,new[]{typeof(SchedulerControl),typeof(SchedulerListEditor)},Platform.Win,new[]{nameof(SchedulerControl.DataBindings)})]
//        [InlineData(PredifinedMap.GridColumn,new[]{typeof(GridColumn),typeof(GridListEditor)},Platform.Win,new[]{nameof(GridColumn.Summary)})]
//        [InlineData(PredifinedMap.GridView,new[]{typeof(GridView),typeof(GridListEditor)},Platform.Win,new[]{nameof(GridView.FormatRules)})]
//        [InlineData(PredifinedMap.PivotGridControl,new[]{typeof(PivotGridControl),typeof(PivotGridListEditor)},Platform.Win,new[]{nameof(PivotGridControl.FormatRules)})]
//        [InlineData(PredifinedMap.PivotGridField,new[]{typeof(PivotGridField),typeof(PivotGridListEditor)},Platform.Win,new[]{nameof(PivotGridField.CustomTotals)})]
//        [InlineData(PredifinedMap.LayoutViewColumn,new[]{typeof(LayoutViewColumn),typeof(GridListEditor)},Platform.Win,new[]{nameof(LayoutViewColumn.Summary)})]
//        [InlineData(PredifinedMap.LayoutView,new[]{typeof(LayoutView),typeof(GridListEditor)},Platform.Win,new[]{nameof(LayoutView.FormatRules)})]
//        [InlineData(PredifinedMap.BandedGridColumn,new[]{typeof(BandedGridColumn),typeof(GridListEditor)},Platform.Win,new[]{nameof(BandedGridColumn.Summary)})]
//        [InlineData(PredifinedMap.AdvBandedGridView,new[]{typeof(AdvBandedGridView),typeof(GridListEditor)},Platform.Win,new[]{nameof(AdvBandedGridView.FormatRules)})]
//        [InlineData(PredifinedMap.ASPxGridView,new[]{typeof(ASPxGridView),typeof(ASPxGridListEditor)},Platform.Web,new[]{nameof(ASPxGridView.Columns)})]
//        [InlineData(PredifinedMap.GridViewColumn,new[]{typeof(GridViewColumn),typeof(ASPxGridListEditor)},Platform.Web,new[]{nameof(GridViewColumn.Columns)})]
//        [InlineData(PredifinedMap.ASPxHtmlEditor,new[]{typeof(ASPxHtmlEditor),typeof(ASPxHtmlPropertyEditor)},Platform.Web,new string[0])]
//        [InlineData(PredifinedMap.TreeList,new[]{typeof(TreeList),typeof(TreeListEditor)},Platform.Win,new string[0])]
//        [InlineData(PredifinedMap.TreeListColumn,new[]{typeof(TreeListColumn),typeof(TreeListEditor)},Platform.Win,new string[0])]
//        [InlineData(PredifinedMap.ASPxScheduler,new[]{typeof(ASPxScheduler),typeof(ASPxSchedulerListEditor)},Platform.Web,new string[0])]
//        [InlineData(PredifinedMap.XafLayoutControl,new[]{typeof(XafLayoutControl)},Platform.Win,new string[0])]
//        [InlineData(PredifinedMap.SplitContainerControl,new[]{typeof(SplitContainerControl)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.DashboardDesigner,new[]{typeof(DashboardDesigner)},Platform.Win,new string[0])]

        internal async Task Map_Predifined_Configurations(PredifinedMap predifinedMap, Type[] assembliesToLoad,Platform platform, string[] collectionNames){
            
            InitializeMapperService($"{nameof(Map_Predifined_Configurations)}{predifinedMap}",platform);
            assembliesToLoad.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();

            var modelType = await predifinedMap.MapToModel().ModelInterfaces();
            var propertyInfos = modelType.GetProperties();

            AssertPredifinedConfigurationsMap(predifinedMap, collectionNames, modelType, propertyInfos);
            AssertBandedGridColumn(predifinedMap, propertyInfos);
            
            AssertSchedulerControl(predifinedMap, propertyInfos);

        }

        [Fact]
        internal async Task Map_PredifinedRepositoryItem_Configurations(){
            var predifinedMaps = Enums.GetValues<PredifinedMap>().Where(map => map.IsRepositoryItem());
            
            foreach (var predifinedMap in predifinedMaps){
                try{
                    InitializeMapperService($"{nameof(Map_PredifinedRepositoryItem_Configurations)}{predifinedMap}",Platform.Win);

                    var replay = predifinedMap.MapToModel().ModelInterfaces().Replay();
                    replay.Connect();
                    await replay;
                    var modelTypes = replay.ToEnumerable().ToArray();
                    
                    var propertyInfos = modelTypes.Last().GetProperties();
                    var descriptionAttribute = propertyInfos.Select(info => info.Attribute<DescriptionAttribute>()).Where(attribute => attribute != null)
                        .FirstOrDefault(attribute =>  attribute.Description.Contains(" "));
                    descriptionAttribute.ShouldNotBeNull();

                    foreach (var modelType in modelTypes){
                        modelType.Property(TypeMappingService.ModelMappersNodeName).ShouldBeNull();    
                    }
                    
                    var modelMapperContainerType = modelTypes.First().ModelMapperContainerTypes().Single();
                    var propertyInfo = modelMapperContainerType.Property(RepositoryItemService.MapName);
                    propertyInfo.ShouldNotBeNull();
                    var listType = propertyInfo.PropertyType.GetInterfaces().FirstOrDefault(type => type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(IModelList<>));
                    listType.ShouldNotBeNull();
                    var baseType = modelTypes.First().Assembly.GetType($"IModel{nameof(RepositoryItemBase)}");
                    listType.GenericTypeArguments.First().ShouldBe(baseType);
                    var realType = modelTypes.First().Assembly.GetTypes().FirstOrDefault(type => type.Name==$"IModel{predifinedMap}");
                    realType.ShouldNotBeNull();
                    realType.GetInterfaces().ShouldContain(baseType);
                    realType.Property(TypeMappingService.ModelMappersNodeName).ShouldBeNull();
                    
                    Dispose();
                }
                catch (Exception e){
                    throw new Exception(predifinedMap.ToString(), e);
                }
            }
        }


        [InlineData(PredifinedMap.ChartControl,new[]{typeof(ChartControl),typeof(ChartListEditor)},Platform.Win,new[]{nameof(ChartControl.Series),"Diagrams"})]
        [InlineData(PredifinedMap.ChartControlDiagram3D,new[]{typeof(Diagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlSimpleDiagram3D,new[]{typeof(SimpleDiagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlFunnelDiagram3D,new[]{typeof(FunnelDiagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlGanttDiagram,new[]{typeof(GanttDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlPolarDiagram,new[]{typeof(PolarDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlRadarDiagram,new[]{typeof(RadarDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlSwiftPlotDiagram,new[]{typeof(SwiftPlotDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlXYDiagram,new[]{typeof(XYDiagram),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlXYDiagram2D,new[]{typeof(XYDiagram2D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        [InlineData(PredifinedMap.ChartControlXYDiagram3D,new[]{typeof(XYDiagram3D),typeof(ChartListEditor)},Platform.Win,new string[0])]
        internal async Task Map_Predifined_ChartControl_Configurations(PredifinedMap configuration,Type[] assembliesToLoad,Platform platform,string[] collectionNames){
            InitializeMapperService($"{nameof(Map_Predifined_ChartControl_Configurations)}{configuration}",platform);
            assembliesToLoad.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();

            var modelType = await configuration.MapToModel().ModelInterfaces();
            var propertyInfos = modelType.GetProperties();
            AssertPredifinedConfigurationsMap(configuration, collectionNames, modelType, propertyInfos);
            if (configuration == PredifinedMap.ChartControl){
                propertyInfos.FirstOrDefault(info => nameof(ChartControl.Diagram) == info.Name).ShouldBeNull();
                var propertyInfo = propertyInfos.FirstOrDefault(info => info.Name == $"{nameof(ChartControl.Diagram)}s");
                propertyInfo.ShouldNotBeNull();
                var type = modelType.Assembly.GetType(typeof(Diagram).ModelMapName(typeof(ChartControl)));
                propertyInfo.PropertyType.GetInterfaces().ShouldContain(typeof(IModelList<>).MakeGenericType(type));
            }
        }

        private void AssertPredifinedConfigurationsMap(PredifinedMap configuration, string[] collectionNames,Type modelType, PropertyInfo[] propertyInfos){
            var modelTypeName = $"IModel{configuration}";
            if (configuration.ToString().StartsWith(PredifinedMap.ChartControl.ToString()) &&
                configuration != PredifinedMap.ChartControl){
                modelTypeName = $"IModel{configuration.ToString().Replace(PredifinedMap.ChartControl.ToString(), "")}";
            }

            modelType.Name.ShouldBe(modelTypeName);

            propertyInfos.Length.ShouldBeGreaterThan(15);
            var descriptionAttribute = propertyInfos.Select(info => info.Attribute<DescriptionAttribute>())
                .FirstOrDefault(attribute => attribute != null && attribute.Description.Contains(" "));
            descriptionAttribute.ShouldNotBeNull();
            foreach (var collectionName in collectionNames){
                propertyInfos.FirstOrDefault(info => info.Name == collectionName).ShouldNotBeNull();
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private void AssertSchedulerControl(PredifinedMap predifinedMap, IList<PropertyInfo> propertyInfos){
            if (predifinedMap == PredifinedMap.SchedulerControl){
                var storageInfo = propertyInfos.FirstOrDefault(info => nameof(SchedulerControl.Storage) == info.Name);
                storageInfo.ShouldNotBeNull();
                var propertyInfo = storageInfo.PropertyType.Property(nameof(SchedulerStorage.Appointments)).PropertyType.Property(nameof(AppointmentStorage.Labels));
                propertyInfo.ShouldNotBeNull();
                
            }
        }

        private static void AssertBandedGridColumn(PredifinedMap configuration, IList<PropertyInfo> propertyInfos){
            if (configuration == PredifinedMap.BandedGridColumn){
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
        internal void Map_All_PredifinedConfigurations(Platform platform){
            
            InitializeMapperService($"{nameof(Map_All_PredifinedConfigurations)}",platform);
            
            var values = Enums.GetValues<PredifinedMap>()
                .Where(map =>map.GetAttributes().OfType<MapPlatformAttribute>().Any(_ => _.Platform == platform.ToString()))
                .ToArray();
            var modelInterfaces = values.MapToModel().ModelInterfaces().Replay();
            modelInterfaces.Connect();

            var types = modelInterfaces.ToEnumerable().ToArray();
            types.Length.ShouldBeGreaterThan(0);

            var valuesLength = values.Length;
            if (platform == Platform.Win){
                valuesLength += 1;
            }
            types.Length.ShouldBe(valuesLength);
            foreach (var configuration in values){
                var name = configuration.ToString();
                if (configuration != PredifinedMap.ChartControl &&
                    configuration.ToString().StartsWith(PredifinedMap.ChartControl.ToString())){
                    name = configuration.ToString().Replace(PredifinedMap.ChartControl.ToString(), "");
                }
                types.FirstOrDefault(_ => _.Name==$"IModel{name}").ShouldNotBeNull();
            }
        }

        [Fact]
        internal void Map_PredifinedConfigurations_Combination(){
            InitializeMapperService($"{nameof(Map_All_PredifinedConfigurations)}",Platform.Win);

            var modelInterfaces = new[]{PredifinedMap.GridView,PredifinedMap.GridColumn}.MapToModel().ModelInterfaces().Replay();
            modelInterfaces.Connect();

            var types = modelInterfaces.ToEnumerable().ToArray();
            types.Length.ShouldBe(2);
            types.First().Name.ShouldBe($"IModel{PredifinedMap.GridView}");
            types.Last().Name.ShouldBe($"IModel{PredifinedMap.GridColumn}");
        }
    }

    
}
