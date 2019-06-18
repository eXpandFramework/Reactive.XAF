using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.ModelMapper.Services.ObjectMapping;
using Xunit;

namespace Tests.Modules.ModelMapper{
    [Collection(nameof(XafTypesInfo))]
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
            Type typeToMap=typeof(ReferenceTypeProperties);
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
    
    }
}