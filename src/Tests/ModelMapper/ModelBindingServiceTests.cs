using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.Web;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.BandedGrid;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.ModelMapper.Tests.BOModel;
using Xunit;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    [Collection(nameof(ModelMapperModule))]
    public class ModelMapperBinderServiceTests:ModelMapperBaseTest{
        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal void Bind_Only_NullAble_Properties_That_are_not_Null(Platform platform){

            var typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_Only_NullAble_Properties_That_are_not_Null)}{typeToMap.Name}{platform}");
            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
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
            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
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
            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
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
            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
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
            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
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
            typeToMap.Extend<IModelListView>();
            var application = DefaultModelMapperModule(platform).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var mapName = typeToMap.ModelMapName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(mapName);
            modelModelMap.GetNode(nameof(ReferenceTypeProperties.RStringValueTypeProperties)).SetValue(nameof(StringValueTypeProperties.RWString),"test");
            var referenceTypeProperties = new ReferenceTypeProperties();

            modelModelMap.BindTo(referenceTypeProperties);

            referenceTypeProperties.RStringValueTypeProperties.RWString.ShouldBe("test");
            
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
        [InlineData(Platform.Win,new[]{PredifinedMap.GridColumn , PredifinedMap.GridView},new[]{typeof(XafGridView),typeof(GridColumn),typeof(GridListEditor)})]
        [InlineData(Platform.Web,new[]{PredifinedMap.GridViewColumn , PredifinedMap.ASPxGridView},new[]{typeof(ASPxGridView),typeof(GridViewColumn),typeof(ASPxGridListEditor)})]
        [InlineData(Platform.Win,new[]{PredifinedMap.BandedGridColumn , PredifinedMap.AdvBandedGridView},new[]{typeof(XafAdvBandedGridView),typeof(BandedGridColumn),typeof(GridListEditor)})]
        [InlineData(Platform.Win,new[]{PredifinedMap.LayoutViewColumn , PredifinedMap.LayoutView},new[]{typeof(XafLayoutView),typeof(LayoutViewColumn),typeof(GridListEditor)})]
        internal async Task Bind_ListEditor_Control(Platform platform,PredifinedMap[] predifinedMaps,Type[] controlTypes){
            controlTypes.ToObservable().Do(type => Assembly.LoadFile(type.Assembly.Location)).Subscribe();
            InitializeMapperService($"{nameof(Bind_ListEditor_Control)}",platform);
            predifinedMaps.Extend();

            var application = DefaultModelMapperModule(platform).Application;
            application.MockListEditor((view, xafApplication, collectionSource) => {
                var listEditor = platform == Platform.Win ? (ListEditor) new CustomGridListEditor(view,controlTypes.First(),controlTypes.Skip(1).First()) : new ASPxGridListEditor(view);
                ((IComplexListEditor) listEditor).Setup(collectionSource, application);
                return listEditor;
            });
            var controlBound = ModelBindingService.ControlBing.Replay();
            controlBound.Connect();

            var listView = application.CreateObjectView<ListView>(typeof(MM));
            listView.CreateControls();

            await controlBound.Take(3).WithTimeOut(TimeSpan.FromSeconds(20));

        }
    
    }
}