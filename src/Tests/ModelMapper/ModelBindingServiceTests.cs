using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows.Forms;
using DevExpress.DashboardWin;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Chart;
using DevExpress.ExpressApp.Chart.Win;
using DevExpress.ExpressApp.Dashboards;
using DevExpress.ExpressApp.Dashboards.Win;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.Scheduler;
using DevExpress.ExpressApp.Scheduler.Win;
using DevExpress.ExpressApp.TreeListEditors;
using DevExpress.ExpressApp.TreeListEditors.Win;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Web;
using DevExpress.Web.ASPxTreeList;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using DevExpress.XtraPivotGrid;
using DevExpress.XtraScheduler;
using DevExpress.XtraTreeList;
using Fasterflect;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.Source.Extensions.XAF.TypesInfo;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.Predifined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;
using Xunit;
using ListView = DevExpress.ExpressApp.ListView;
using Task = System.Threading.Tasks.Task;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    [Collection(nameof(ModelMapperModule))]
    public class ModelMapperBinderServiceTests:ModelMapperBaseTest{
        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Bind_Only_NullAble_Properties_That_are_not_Null(Platform platform){

            var typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_Only_NullAble_Properties_That_are_not_Null)}{typeToMap.Name}{platform}");

            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWInteger),100);
            var stringValueTypeProperties = new StringValueTypeProperties{RWString = "shouldnotchange"};

            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWInteger.ShouldBe(100);
            stringValueTypeProperties.RWString.ShouldBe("shouldnotchange");
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Do_not_bind_Disable_mode_nodes(Platform platform){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Do_not_bind_Disable_mode_nodes)}{typeToMap.Name}{platform}");
            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
 
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWInteger),100);
            var stringValueTypeProperties = new StringValueTypeProperties{RWString = "shouldnotchange"};

            modelModelMap.NodeDisabled = true;
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWString.ShouldBe("shouldnotchange");
            stringValueTypeProperties.RWInteger.ShouldBe(0);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Do_not_throw_if_target_object_properties_do_not_exist(Platform platform){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Do_not_throw_if_target_object_properties_do_not_exist)}{typeToMap.Name}{platform}");
            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.Index = 100;
            var stringValueTypeProperties = new StringValueTypeProperties();

            modelModelMap.BindTo(stringValueTypeProperties);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Bind_all_public_nullable_type_properties(Platform platform){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_all_public_nullable_type_properties)}{typeToMap.Name}{platform}");
            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
 
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWInteger),100);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.NullAbleRWInteger),200);
            var stringValueTypeProperties = new StringValueTypeProperties();
            
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWInteger.ShouldBe(100);
            stringValueTypeProperties.NullAbleRWInteger.ShouldBe(200);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Bind_all_public_rw_string_properties(Platform platform){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_all_public_rw_string_properties)}{typeToMap.Name}{platform}");
            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
 
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWString),"test");
            var stringValueTypeProperties = new StringValueTypeProperties();
            
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWString.ShouldBe("test");

        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Bind_all_public_rw_nested_properties(Platform platform){
            var typeToMap=typeof(ReferenceTypeProperties);
            InitializeMapperService($"{nameof(Bind_all_public_rw_nested_properties)}{typeToMap.Name}{platform}");
            var module = typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform,module).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            
            var modelModelMap = (IModelModelMap)modelListView.MapNode(typeToMap);
            modelModelMap.GetNode(nameof(ReferenceTypeProperties.RStringValueTypeProperties)).SetValue(nameof(StringValueTypeProperties.RWString),"test");
            var referenceTypeProperties = new ReferenceTypeProperties();

            modelModelMap.BindTo(referenceTypeProperties);

            referenceTypeProperties.RStringValueTypeProperties.RWString.ShouldBe("test");
            
        }

        [Theory]
        [InlineData(Platform.Win,Skip = NotImplemented)]
//        [InlineData(Platform.Web)]
        internal void Bind_IEnumerable_Properties(Platform platform){
            
//            var typeToMap=typeof(CollectionsType);
//            InitializeMapperService($"{nameof(Bind_IEnumerable_Properties)}{typeToMap.Name}{platform}");
//            var module = typeToMap.Extend<IModelListView>();
//            var application = DefaultModelMapperModule(platform,module).Application;
//            var modelListView = application.Model.Views.OfType<IModelListView>().First();
//            var mapName = typeToMap.ModelMapName();
//            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
//            var modelNode = modelModelMap.GetNode(nameof(CollectionsType.TestModelMappersList)).AddNode();
//            modelNode.SetValue(nameof(TestModelMapper.Name),"Test");
//
//            var collectionsType = new CollectionsType();
//            var testModelMapper = new TestModelMapper();
//            collectionsType.TestModelMappersList.Add(testModelMapper);
//
//            modelModelMap.BindTo(collectionsType);
//
//            testModelMapper.Name.ShouldBe("Test");
            
        }

        [Theory]
        [InlineData(Platform.Win,Skip = NotImplemented)]
        [InlineData(Platform.Web,Skip = NotImplemented)]
        internal void Apply_AllMapper_Contexts(Platform platform){
            
        }

        [Theory]
        [InlineData(Platform.Win,Skip = NotImplemented)]
        [InlineData(Platform.Web,Skip = NotImplemented)]
        internal void Apply_Root_Map_After_mapper_contexts(Platform platform){
            
        }

        [Theory]
        [InlineData(Platform.Web,new[]{PredifinedMap.ASPxPopupControl },new[]{typeof(ASPxPopupControl)},new Type[0],0)]
        [InlineData(Platform.Win,new[]{PredifinedMap.XafLayoutControl },new[]{typeof(XafLayoutControl)},new Type[0],0)]
        internal async Task Bind_DetailView_Maps(Platform platform,PredifinedMap[] predifinedMaps,Type[] controlTypes,Type[] extraModules,int boundTypes){
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService($"{nameof(Bind_DetailView_Maps)}{predifinedMaps.First()}",platform);
            

            var module = predifinedMaps.Extend();
            var application = DefaultModelMapperModule(platform,extraModules.Select(_ => {
                var instance = _.CreateInstance();
                return instance;
            }).Cast<ModuleBase>().Concat(new[]{module}).ToArray()).Application;

            var controlBound = ModelBindingService.ControlBind.Replay();
            controlBound.Connect();
            
            var detailView = application.CreateObjectView<DetailView>(typeof(MM));
            detailView.CreateControls();
            var task = controlBound.Take(boundTypes).WithTimeOut(TimeSpan.FromSeconds(10));
            if (boundTypes>0){
                await task;
            }

        }

        [Theory]
        [InlineData(Platform.Win,new[]{PredifinedMap.GridColumn , PredifinedMap.GridView},new[]{typeof(XafGridView),typeof(GridColumn),typeof(GridListEditor)},new Type[0],3)]
        [InlineData(Platform.Web,new[]{PredifinedMap.GridViewColumn , PredifinedMap.ASPxGridView},new[]{typeof(ASPxGridView),typeof(GridViewColumn),typeof(ASPxGridListEditor)},new Type[0],3)]
        [InlineData(Platform.Win,new[]{PredifinedMap.BandedGridColumn , PredifinedMap.AdvBandedGridView},new[]{typeof(XafAdvBandedGridView),typeof(BandedGridColumn),typeof(GridListEditor)},new Type[0],3)]
        
        [InlineData(Platform.Win,new[]{PredifinedMap.LayoutViewColumn , PredifinedMap.LayoutView},new[]{typeof(XafLayoutView),typeof(LayoutViewColumn),typeof(CustomGridListEditor)},new Type[0],3)]
        [InlineData(Platform.Win,new[]{PredifinedMap.SplitContainerControl },new[]{typeof(SplitContainerControl)},new Type[0],1)]
        [InlineData(Platform.Web,new[]{PredifinedMap.ASPxPopupControl },new[]{typeof(ASPxPopupControl)},new Type[0],0)]
        [InlineData(Platform.Win,new[]{PredifinedMap.TreeListColumn , PredifinedMap.TreeList},new[]{typeof(TreeList),typeof(TreeListColumn),typeof(TreeListEditor)},new[]{typeof(TreeListEditorsModuleBase),typeof(TreeListEditorsWindowsFormsModule)},3)]
        [InlineData(Platform.Win,new[]{PredifinedMap.DashboardDesigner },new[]{typeof(DashboardDesigner)},new[]{typeof(DashboardsModule),typeof(DashboardsWindowsFormsModule)},0)]

        [InlineData(Platform.Win, new[]{PredifinedMap.PivotGridField, PredifinedMap.PivotGridControl},new[]{typeof(PivotGridControl), typeof(PivotGridListEditor)},new[]{typeof(PivotGridModule), typeof(PivotGridWindowsFormsModule)}, 3)]
        [InlineData(Platform.Win, new[]{PredifinedMap.ChartControl},new[]{typeof(ChartControl), typeof(ChartListEditor)},new[]{typeof(ChartModule), typeof(ChartWindowsFormsModule)},1)]
        [InlineData(Platform.Win, new[]{PredifinedMap.SchedulerControl},new[]{typeof(SchedulerControl), typeof(SchedulerListEditor)},new[]{typeof(SchedulerModuleBase), typeof(SchedulerWindowsFormsModule)}, 1)]
        internal async Task Bind_ListView_Maps(Platform platform,PredifinedMap[] predifinedMaps,Type[] controlTypes,Type[] extraModules,int boundTypes){
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService($"{nameof(Bind_ListView_Maps)}{predifinedMaps.First()}",platform);
            var predifinedMap = predifinedMaps.Last();
            
            var module = predifinedMaps.Extend();

            var application = DefaultModelMapperModule(platform,extraModules.Select(_ => {
                var instance = _.CreateInstance();
                if (instance is DashboardsModule dashboardsModule){
                    dashboardsModule.DashboardDataType = typeof(DashboardData);
                }
                return instance;
            }).Cast<ModuleBase>().Concat(new[]{module}).ToArray()).Application;
            MockListEditor(platform, controlTypes, application, predifinedMap,null);
            var controlBound = ModelBindingService.ControlBind.Replay();
            controlBound.Connect();
            
            var listView = application.CreateObjectView<ListView>(typeof(MM));
            listView.Model.EditorType = controlTypes.Last();
            listView.Model.BandsLayout.Enable = predifinedMaps.Contains(PredifinedMap.AdvBandedGridView);

            listView.CreateControls();
            if (boundTypes>0){
                await controlBound.Take(boundTypes).WithTimeOut(TimeSpan.FromSeconds(10));
            }

        }
        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Bind_PropertyEditor_Control(Platform platform){
            
            var maps = EnumsNET.Enums.GetValues<PredifinedMap>().Where(map => map.IsPropertyEditor()&&map.Attribute<MapPlatformAttribute>().Platform==platform.ToString());
            foreach (var predifinedMap in maps){
                try{
                    var controlType = predifinedMap.Assembly().GetType(predifinedMap.GetTypeName());
                    InitializeMapperService($"{nameof(Bind_PropertyEditor_Control)}{predifinedMap}",platform);
                    var module = predifinedMap.Extend();
                    var application = DefaultModelMapperModule(platform,predifinedMap.Modules().Select(_ => {
                        var instance = _.CreateInstance();
                        if (instance is DashboardsModule dashboardsModule){
                            dashboardsModule.DashboardDataType = typeof(DashboardData);
                        }
                        return instance;
                    }).Cast<ModuleBase>().Concat(new[]{module}).ToArray()).Application;
            
                    var controlBound = ModelBindingService.ControlBind.Replay();
                    controlBound.Connect();
                    var modelPropertyEditor = ((IModelPropertyEditor) application.Model.GetNodeByPath(MMDetailViewTestItemNodePath));
                    var mapNode = modelPropertyEditor.GetNode(ViewItemService.PropertyEditorControlMapName);
                    var propertyEditorControlType = mapNode.ModelListItemType().ToTypeInfo();
                    var typeInfo = propertyEditorControlType.Descendants.First(info => info.Type.Name==predifinedMap.ModelTypeName());
                    mapNode.AddNode(typeInfo.Type);
                    application.MockDetailViewEditor( modelPropertyEditor, controlType.CreateInstance());

                    var detailView = application.CreateObjectView<DetailView>(typeof(MM));
            
                    detailView.DelayedItemsInitialization = false;
                    detailView.CreateControls();

                    await controlBound.Take(1).WithTimeOut(TimeSpan.FromSeconds(10)); 
                    application.Dispose();
                    Dispose();
                }
                catch (Exception e){
                    throw new Exception(predifinedMap.ToString(), e);
                }
            }
            

        }
        [Theory]
        [InlineData(typeof(ListView))]
        [InlineData(typeof(DetailView))]
        internal async Task Bind_RepositoryItems(Type viewType){
            var boundTypes = 2;
            var controlTypes =new[]{typeof(XafGridView),typeof(GridColumn)};
            var predifinedMaps = new[]{PredifinedMap.RepositoryItem,PredifinedMap.RepositoryItemTextEdit };
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService($"{nameof(Bind_RepositoryItems)}{predifinedMaps.First()}",Platform.Win);
            var module = predifinedMaps.Extend();
            var application = DefaultModelMapperModule(Platform.Win,module).Application;
            var controlBound = ModelBindingService.ControlBind.Replay();
            controlBound.Connect();
            
            var modelPropertyEditor = ((IModelPropertyEditor) application.Model.GetNodeByPath(MMDetailViewTestItemNodePath));
            var controlInstance = new StringEdit();
            var repositoryItemTextEdit = controlInstance.Properties;
            if (viewType == typeof(ListView)){
                MockListEditor(Platform.Win, controlTypes, application, PredifinedMap.GridView,repositoryItemTextEdit);
            }
            else{
                application.MockDetailViewEditor( modelPropertyEditor, controlInstance);
            }

            var objectView = application.CreateObjectView(viewType,typeof(MM));
            var values = ConfigureModelRepositories(objectView);
            objectView.DelayedItemsInitialization = false;
            objectView.CreateControls();

            await controlBound.Take(boundTypes).WithTimeOut(TimeSpan.FromSeconds(10));

            Debug.Assert(repositoryItemTextEdit != null, nameof(repositoryItemTextEdit) + " != null");
            repositoryItemTextEdit.CharacterCasing.ShouldBe(values.concreteTypePropertyValue);
            repositoryItemTextEdit.AccessibleDescription.ShouldBe(values.basePropertyValue);

        }

        private static (CharacterCasing concreteTypePropertyValue, string basePropertyValue) ConfigureModelRepositories(ObjectView objectView){
            var concreteTypePropertyValue = CharacterCasing.Upper;
            var basePropertyValue = "AccessibleDescription";
            var repositoryItemsNode = objectView.Model.Items(nameof(MM.Test)).Cast<IModelNode>().First().GetNode(ViewItemService.RepositoryItemsMapName);
//            var descendants = repositoryItemsNode.GetType().GetInterfaces()
//                .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IModelList<>))
//                .Select(type => type.GenericTypeArguments.First()).First().ToTypeInfo().Descendants.ToArray();
            var descendants = repositoryItemsNode.ModelListItemType().ToTypeInfo().Descendants.ToArray();
            var repositoryItemModelType = descendants.First().Type;
            var baseRepoNode = repositoryItemsNode.AddNode(repositoryItemModelType, "BaseRepo");
            (baseRepoNode is IModelModelMap).ShouldBeTrue();
            baseRepoNode.SetValue(nameof(RepositoryItem.AccessibleDescription), basePropertyValue);
            var repositoryTextItemModelType = descendants.Last();
            var textEditNode = repositoryItemsNode.AddNode(repositoryTextItemModelType);
            textEditNode.SetValue(nameof(RepositoryItemTextEdit.CharacterCasing), concreteTypePropertyValue);
            return (concreteTypePropertyValue, basePropertyValue);
        }


        private static void MockListEditor(Platform platform, Type[] controlTypes, XafApplication application,
            PredifinedMap predifinedMap, RepositoryItemTextEdit repositoryItemTextEdit){
            application.MockListEditor((view, xafApplication, collectionSource) => {
                ListEditor listEditor;
                if (new[]{PredifinedMap.PivotGridControl,PredifinedMap.ChartControl,PredifinedMap.SchedulerControl,PredifinedMap.TreeList, }.Any(map => map==predifinedMap)){
                    listEditor =
                        (ListEditor) Activator.CreateInstance(controlTypes.Last(), view);
                }
                else if (new[]{PredifinedMap.DashboardDesigner,PredifinedMap.SplitContainerControl}.Any(map => map == predifinedMap)){
                    return application.ListEditorMock().Object;
                }
                else
                    listEditor = platform == Platform.Win
                        ? (ListEditor) new CustomGridListEditor(view, controlTypes.First(),
                            controlTypes.Skip(1).First(),repositoryItemTextEdit)
                        : new ASPxGridListEditor(view);

                if (listEditor is IComplexListEditor complexListEditor){
                    complexListEditor.Setup(collectionSource, application);
                }

                return listEditor;
            });
        }
    }

}