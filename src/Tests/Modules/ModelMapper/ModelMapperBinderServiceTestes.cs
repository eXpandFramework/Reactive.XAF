using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Shouldly;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xunit;

namespace Tests.Modules.ModelMapper{
    [Xunit.Collection(nameof(XafTypesInfo))]
    public class ModelMapperBinderServiceTestes:ModelMapperBaseTest{
        [Fact]
        public void Bind_Only_NullAble_Properties_That_are_not_Null(){
            var typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_Only_NullAble_Properties_That_are_not_Null)}{typeToMap.Name}");
            typeToMap.MapToModel().Extend<IModelListView>();
            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(cleanCodeName);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWInteger),100);
            var stringValueTypeProperties = new StringValueTypeProperties{RWString = "shouldnotchange"};

            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWInteger.ShouldBe(100);
            stringValueTypeProperties.RWString.ShouldBe("shouldnotchange");
        }

        [Fact]
        public void Do_not_bind_Disable_mode_nodes(){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Do_not_bind_Disable_mode_nodes)}{typeToMap.Name}");
            typeToMap.MapToModel().Extend<IModelListView>();
            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(cleanCodeName);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWInteger),100);
            var stringValueTypeProperties = new StringValueTypeProperties{RWString = "shouldnotchange"};

            modelModelMap.NodeDisabled = true;
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWString.ShouldBe("shouldnotchange");
            stringValueTypeProperties.RWInteger.ShouldBe(0);
        }

        [Fact]
        public void Do_not_throw_if_target_object_properties_do_not_exist(){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Do_not_throw_if_target_object_properties_do_not_exist)}{typeToMap.Name}");
            typeToMap.MapToModel().Extend<IModelListView>();
            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(cleanCodeName);
            modelModelMap.Index = 100;
            var stringValueTypeProperties = new StringValueTypeProperties();

            modelModelMap.BindTo(stringValueTypeProperties);
        }

        [Fact]
        public void Bind_all_public_nullable_type_properties(){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_all_public_nullable_type_properties)}{typeToMap.Name}");
            typeToMap.MapToModel().Extend<IModelListView>();
            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(cleanCodeName);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWInteger),100);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.NullAbleRWInteger),200);
            var stringValueTypeProperties = new StringValueTypeProperties();
            
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWInteger.ShouldBe(100);
            stringValueTypeProperties.NullAbleRWInteger.ShouldBe(200);
        }

        [Fact]
        public void Bind_all_public_rw_string_properties(){
            Type typeToMap=typeof(StringValueTypeProperties);
            InitializeMapperService($"{nameof(Bind_all_public_rw_string_properties)}{typeToMap.Name}");
            typeToMap.MapToModel().Extend<IModelListView>();
            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(cleanCodeName);
            modelModelMap.SetValue(nameof(StringValueTypeProperties.RWString),"test");
            var stringValueTypeProperties = new StringValueTypeProperties();
            
            modelModelMap.BindTo(stringValueTypeProperties);

            stringValueTypeProperties.RWString.ShouldBe("test");

        }

        [Fact]
        public void Bind_all_public_rw_nested_properties(){
            Type typeToMap=typeof(ReferenceTypeProperties);
            InitializeMapperService($"{nameof(Bind_all_public_rw_nested_properties)}{typeToMap.Name}");
            typeToMap.MapToModel().Extend<IModelListView>();
            var application = DefaultModelMapperModule(Platform.Agnostic).Application;
            var modelListView = application.Model.Views.OfType<IModelListView>().First();
            var cleanCodeName = typeToMap.FullName.CleanCodeName();
            var modelModelMap = (IModelModelMap)modelListView.GetNode(cleanCodeName);
            modelModelMap.GetNode(nameof(ReferenceTypeProperties.RStringValueTypeProperties)).SetValue(nameof(StringValueTypeProperties.RWString),"test");
            var referenceTypeProperties = new ReferenceTypeProperties();

            modelModelMap.BindTo(referenceTypeProperties);

            referenceTypeProperties.RStringValueTypeProperties.RWString.ShouldBe("test");
            
        }

        [Fact]
        public void Apply_AllMapper_Contexts(){
            throw new NotImplementedException();            
        }

        [Fact]
        public void Apply_Root_Map_After_mapper_contexts(){
            throw new NotImplementedException();            
        }
    
    }
}