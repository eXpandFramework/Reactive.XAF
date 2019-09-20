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
using NUnit.Framework;
using Shouldly;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Tests.TypeMappingServiceTests{
    [NonParallelizable]
    public class MapTests:ModelMapperBaseTest{
        [Test]
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

        
        [TestCase(typeof(CollectionsType), new[] {
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


        [Test]
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

        [Test]
        public async Task Map_Nested_type_properties(){
            InitializeMapperService(nameof(Map_Nested_type_properties));
            var typeToMap = typeof(NestedTypeProperties);

            var modelType = await typeToMap.MapToModel().ModelInterfaces();

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(1);
        }

        [Test]
        public async Task Map_Multiple_Objects_from_the_same_subscription_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_from_the_same_subscription_In_the_same_assembly));

            var mappedTypes = new[]{typeToMap1, typeToMap2}.MapToModel().ModelInterfaces();

            var mappedType1 = await mappedTypes.FirstAsync(type => typeToMap1.ModelTypeName()==type.Name).Timeout(Timeout);
            var mappedType2 = await mappedTypes.FirstAsync(type => typeToMap2.ModelTypeName()==type.Name).Timeout(Timeout);

            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Test]
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

        [Test]
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

        [TestCase(PredefinedMap.GridColumn,new[]{typeof(GridColumn),typeof(GridListEditor)},nameof(Platform.Win),new[]{nameof(GridColumn.Summary)})]
        [TestCase(PredefinedMap.GridView,new[]{typeof(GridView),typeof(GridListEditor)},nameof(Platform.Win),new[]{nameof(GridView.FormatRules)})]
        [TestCase(PredefinedMap.PivotGridControl,new[]{typeof(PivotGridControl),typeof(PivotGridListEditor)},nameof(Platform.Win),new[]{nameof(PivotGridControl.FormatRules)})]
        [TestCase(PredefinedMap.PivotGridField,new[]{typeof(PivotGridField),typeof(PivotGridListEditor)},nameof(Platform.Win),new[]{nameof(PivotGridField.CustomTotals)})]
        [TestCase(PredefinedMap.LayoutViewColumn,new[]{typeof(LayoutViewColumn),typeof(GridListEditor)},nameof(Platform.Win),new[]{nameof(LayoutViewColumn.Summary)})]
        [TestCase(PredefinedMap.LayoutView,new[]{typeof(LayoutView),typeof(GridListEditor)},nameof(Platform.Win),new[]{nameof(LayoutView.FormatRules)})]
        [TestCase(PredefinedMap.BandedGridColumn,new[]{typeof(BandedGridColumn),typeof(GridListEditor)},nameof(Platform.Win),new[]{nameof(BandedGridColumn.Summary)})]
        [TestCase(PredefinedMap.AdvBandedGridView,new[]{typeof(AdvBandedGridView),typeof(GridListEditor)},nameof(Platform.Win),new[]{nameof(AdvBandedGridView.FormatRules)})]
        [TestCase(PredefinedMap.ASPxGridView,new[]{typeof(ASPxGridView),typeof(ASPxGridListEditor)},nameof(Platform.Web),new[]{nameof(ASPxGridView.Columns)})]
        [TestCase(PredefinedMap.GridViewColumn,new[]{typeof(GridViewColumn),typeof(ASPxGridListEditor)},nameof(Platform.Web),new[]{nameof(GridViewColumn.Columns)})]
        [TestCase(PredefinedMap.ASPxHtmlEditor,new[]{typeof(ASPxHtmlEditor),typeof(ASPxHtmlPropertyEditor)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.TreeList,new[]{typeof(TreeList),typeof(TreeListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.TreeListColumn,new[]{typeof(TreeListColumn),typeof(TreeListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.SchedulerControl,new[]{typeof(SchedulerControl),typeof(SchedulerListEditor)},nameof(Platform.Win),new[]{nameof(SchedulerControl.DataBindings)})]
        [TestCase(PredefinedMap.ASPxScheduler,new[]{typeof(ASPxScheduler),typeof(ASPxSchedulerListEditor)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.XafLayoutControl,new[]{typeof(XafLayoutControl)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.SplitContainerControl,new[]{typeof(SplitContainerControl)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.DashboardDesigner,new[]{typeof(DashboardDesigner)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ASPxPopupControl,new[]{typeof(ASPxPopupControl)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.DashboardViewer,new[]{typeof(DashboardViewer)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ASPxDashboard,new[]{typeof(ASPxDashboard)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxDateEdit,new[]{typeof(ASPxDateEdit)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxHyperLink,new[]{typeof(ASPxHyperLink)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxLookupDropDownEdit,new[]{typeof(ASPxLookupDropDownEdit)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxLookupFindEdit,new[]{typeof(ASPxLookupFindEdit)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxSpinEdit,new[]{typeof(ASPxSpinEdit)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxTokenBox,new[]{typeof(ASPxTokenBox)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.ASPxComboBox,new[]{typeof(ASPxComboBox)},nameof(Platform.Web),new string[0])]
        [TestCase(PredefinedMap.LabelControl,new[]{typeof(LabelControl)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.RichEditControl,new[]{typeof(RichEditControl)},nameof(Platform.Win),new string[0])]
        public async Task Map_Predefined_Configurations(PredefinedMap predefinedMap, Type[] assembliesToLoad,string platformName, string[] collectionNames){
            var platform = GetPlatform(platformName);
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
        [TestCase(nameof(Platform.Win))]
        public async Task Map_PredefinedMap_RepositoryItems(string platformName){
            var platform = GetPlatform(platformName);
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
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task Map_PredefinedMap_PropertyEditor_Controls(string platformName){
            var platform = GetPlatform(platformName);
            var predefinedMaps = Enums.GetValues<PredefinedMap>().Where(map => map.IsPropertyEditor())
                .Where(map => map.Attribute<MapPlatformAttribute>().Platform==platform.ToString());
            
            await Map_PredefinedMap_ViewItems(platform, predefinedMaps, typeof(PropertyEditorControlMap).ModelTypeName(), ViewItemService.PropertyEditorControlMapName);
        }

        [Theory]
        [TestCase(PredefinedMap.ChartControl,new[]{typeof(ChartControl),typeof(ChartListEditor)},nameof(Platform.Win),new[]{nameof(ChartControl.Series),"Diagrams"})]
        [TestCase(PredefinedMap.ChartControlDiagram3D,new[]{typeof(Diagram3D),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlSimpleDiagram3D,new[]{typeof(SimpleDiagram3D),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlFunnelDiagram3D,new[]{typeof(FunnelDiagram3D),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlGanttDiagram,new[]{typeof(GanttDiagram),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlPolarDiagram,new[]{typeof(PolarDiagram),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlRadarDiagram,new[]{typeof(RadarDiagram),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlSwiftPlotDiagram,new[]{typeof(SwiftPlotDiagram),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlXYDiagram,new[]{typeof(XYDiagram),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlXYDiagram2D,new[]{typeof(XYDiagram2D),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        [TestCase(PredefinedMap.ChartControlXYDiagram3D,new[]{typeof(XYDiagram3D),typeof(ChartListEditor)},nameof(Platform.Win),new string[0])]
        public async Task Map_Predefined_ChartControl_Configurations(PredefinedMap configuration,Type[] assembliesToLoad,string platformName,string[] collectionNames){
            var platform = GetPlatform(platformName);
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
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public void Map_All_PredefinedConfigurations(string platformName){
            var platform = GetPlatform(platformName);
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

        [Test]
        public void Map_PredefinedConfigurations_Combination(){
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
