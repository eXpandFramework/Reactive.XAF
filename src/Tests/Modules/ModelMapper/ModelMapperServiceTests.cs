using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.XtraGrid.Views.Grid;
using Fasterflect;
using Shouldly;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper;
using Xunit;

namespace Tests.Modules.ModelMapper{
    [Xunit.Collection(nameof(XafTypesInfo))]
    public class ModelMapperServiceTests:ModelMapperBaseTest{

        [Fact]
        public async Task Map_RW_StringValueType_Public_Properties(){
            InitializeMapperService(nameof(Map_RW_StringValueType_Public_Properties));
            var typeToMap = typeof(StringValueTypeProperties);
            var propertiesToMap = typeToMap.Properties().Where(info => info.CanRead&&info.CanWrite).ToArray();

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

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


        private static PropertyInfo[] ModelTypeProperties(Type modelType){
            return modelType.Properties().Where(info =>
                !ModelMapperService.ReservedPropertyNames.Contains(info.Name) &&
                !info.Name.EndsWith(ModelMapperService.ContainerSuffix)&&info.Name!=ModelMapperService.ModelMappersNodeName).ToArray();
        }

        [Fact]
        public async Task Map_All_ReferenceType_Public_Properties(){
            InitializeMapperService(nameof(Map_All_ReferenceType_Public_Properties));
            var typeToMap = typeof(ReferenceTypeProperties);
            var propertiesToMap = typeToMap.Properties().ToArray();

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            foreach (var propertyInfo in propertiesToMap){
                var modelProperty = modelTypeProperties.FirstOrDefault(info => info.Name==propertyInfo.Name);
                modelProperty.ShouldNotBeNull(propertyInfo.Name);
                modelProperty.PropertyType.Name.ShouldBe($"IModel{propertyInfo.PropertyType.FullName.CleanCodeName()}");
            }

            modelTypeProperties.Length.ShouldBe(propertiesToMap.Length);
        }

        [Fact]
        public async Task Do_Not_Map_Already_Mapped_Properties(){
            InitializeMapperService(nameof(Do_Not_Map_Already_Mapped_Properties));
            var typeToMap = typeof(SelfReferenceTypeProperties);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var modelTypeProperties = modelType.Properties();

            modelTypeProperties.FirstOrDefault(info => info.Name==nameof(SelfReferenceTypeProperties.Self)).ShouldBeNull();
            var nestedType = modelType.Assembly.GetType($"IModel{typeof(NestedSelfReferenceTypeProperties).FullName.CleanCodeName()}");
            nestedType.ShouldNotBeNull();
            nestedType.Properties().Count.ShouldBe(0);
        }

        [Fact]
        public async Task Map_Nested_type_properties(){
            InitializeMapperService(nameof(Map_Nested_type_properties));
            var typeToMap = typeof(NestedTypeProperties);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(1);

        }

        [Fact]
        public async Task Do_Not_Map_Reserved_properties(){
            InitializeMapperService(nameof(Do_Not_Map_Reserved_properties));
            var typeToMap = typeof(ResevredProperties);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var modelTypeProperties = ModelTypeProperties(modelType);
            
            modelTypeProperties.Length.ShouldBe(0);

        }

        [Fact]
        public async Task Assembly_Version_Should_Match_Model_Mapper_Version(){
            InitializeMapperService(nameof(Assembly_Version_Should_Match_Model_Mapper_Version));
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var modelMapperVersion = typeof(ModelMapperModule).Assembly.GetName().Version;
            modelType.Assembly.GetName().Version.ShouldBe(modelMapperVersion);
        }
        
        [Theory]
        [InlineData(typeof(GridView))]
        public async Task Map_DevExpress_Types(Type typeToMap){
            InitializeMapperService(nameof(Map_DevExpress_Types));

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            modelType.Name.ShouldBe($"IModel{typeToMap.FullName.CleanCodeName()}");
            
        }

        [Fact]
        public async Task Map_Multiple_Objects_In_the_same_assembly(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(StringValueTypeProperties);
            InitializeMapperService(nameof(Map_Multiple_Objects_In_the_same_assembly));

            
            new[]{typeToMap1,typeToMap2}.MapToModel();

            var mappedTypes = ModelMapperService.MappedTypes.Replay();
            mappedTypes.Connect();
            var mappedType1 = await mappedTypes.Take(1);
            mappedType1.Name.ShouldBe($"IModel{typeToMap1.FullName.CleanCodeName()}");
            var mappedType2 = await mappedTypes.Take(2);
            mappedType2.Name.ShouldBe($"IModel{typeToMap2.FullName.CleanCodeName()}");
            mappedType1.Assembly.ShouldBe(mappedType2.Assembly);
        }

        [Fact]
        public void Do_Not_Map_Already_Mapped_Types(){
            var typeToMap1 = typeof(TestModelMapper);
            var typeToMap2 = typeof(TestModelMapper);
            InitializeMapperService(nameof(Do_Not_Map_Already_Mapped_Types));

            typeToMap1.MapToModel();
            typeToMap2.MapToModel();
            
            ModelMapperService.MappedTypes.ToEnumerable().Count().ShouldBe(1);
            
        }

        [Fact]
        public async Task Custom_Container_Name(){
            InitializeMapperService(nameof(Custom_Container_Name));
            var typeToMap = typeof(TestModelMapper);
            var codeName = typeToMap.FullName.CleanCodeName();
            var customContainerName = "Custom";
            string customContainerPersistentName="customContainerPersistentName";

            typeToMap.MapToModel(new ModelMapperConfiguration(){CustomContainerName = customContainerName,CustomContainerPersistentName = customContainerPersistentName});
            var modelType = await ModelMapperService.MappedTypes;

            var containerType = modelType.Assembly.GetType($"IModel{codeName}Container");
            var propertyInfo = containerType.Properties().First();
            propertyInfo.Name.ShouldBe(customContainerName);
            var modelPersistentNameAttribute = propertyInfo.Attribute<ModelPersistentNameAttribute>();
            modelPersistentNameAttribute.ShouldNotBeNull();
            modelPersistentNameAttribute.Name.ShouldBe(customContainerPersistentName);
        }

        [Fact]
        public async Task Custom_Container_Image(){
            InitializeMapperService(nameof(Custom_Container_Image));
            var typeToMap = typeof(TestModelMapper);
            var codeName = typeToMap.FullName.CleanCodeName();
            var imageName = "ImageName";
            
            typeToMap.MapToModel(new ModelMapperConfiguration(){ImageName = imageName});
            var modelType = await ModelMapperService.MappedTypes;

            var containerType = modelType.Assembly.GetType($"IModel{codeName}Container");
            var imageNameAttribute = containerType.Attribute<ImageNameAttribute>();
            imageNameAttribute.ShouldNotBeNull();
            imageNameAttribute.ImageName.ShouldBe(imageName);
        }

        [Fact]
        public async Task Copy_Attributes(){
            InitializeMapperService(nameof(Copy_Attributes));
            var typeToMap = typeof(CopyAttributesClass);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var propertyInfos = modelType.Properties();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeNoParam))
                .GetCustomAttributes(typeof(DescriptionAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributePrivate))
                .GetCustomAttributes(typeof(Attribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeValueTypeParam))
                .GetCustomAttributes(typeof(IndexAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeDefaultVvalueAttribue))
                .GetCustomAttributes(typeof(DefaultValueAttribute),false).FirstOrDefault().ShouldBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeStringParam))
                .GetCustomAttributes(typeof(DescriptionAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTwoParam))
                .GetCustomAttributes(typeof(MyClassAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeTypeParam))
                .GetCustomAttributes(typeof(TypeConverterAttribute),false).FirstOrDefault().ShouldNotBeNull();
            propertyInfos.First(info => info.Name==nameof(CopyAttributesClass.AttributeEnumParam))
                .GetCustomAttributes(typeof(MyClassAttribute),false).FirstOrDefault().ShouldNotBeNull();
        }

        [Fact]
        public async Task Copy_Private_DescriptionAttributes(){
            InitializeMapperService(nameof(Copy_Private_DescriptionAttributes));
            
            var typeToMap = typeof(PrivateDescriptionAttributesClass);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            modelType.Properties().First().GetCustomAttributes(typeof(DescriptionAttribute),false).Cast<DescriptionAttribute>().Any().ShouldBeTrue();
            
        }

        [Fact]
        public async Task Attributes_Can_Be_Replaced(){
            InitializeMapperService(nameof(Attributes_Can_Be_Replaced));
            ModelMapperService.AttributesMap.Add(typeof(PrivateAttribute),(typeof(DescriptionAttribute),attribute => null));
            var typeToMap = typeof(ReplaceAttributesClass);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            modelType.Properties().First().GetCustomAttributes(typeof(DescriptionAttribute),false).Cast<DescriptionAttribute>().Any().ShouldBeTrue();
            
        }

        [Fact]
        public async Task Container_Interface(){
            InitializeMapperService(nameof(Container_Interface));
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;

            var codeName = typeof(TestModelMapper).FullName.CleanCodeName();
            var containerType = modelType.Assembly.GetType($"IModel{codeName}{ModelMapperService.ContainerSuffix}");
            containerType.ShouldNotBeNull();
            var propertyInfo = containerType.GetProperty(codeName);
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();

        }

        [Fact]
        public async Task ModelMappers_Interface(){
            InitializeMapperService(nameof(ModelMappers_Interface));
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel();
            var modelType = await ModelMapperService.MappedTypes;
            
            var containerName = typeof(TestModelMapper).FullName.CleanCodeName();
            var containerType = modelType.Assembly.GetType($"IModel{containerName}{ModelMapperService.ContainerSuffix}");
            
            var propertyInfo = containerType.GetProperty(containerName)?.PropertyType.GetProperty(ModelMapperService.ModelMappersNodeName);
            propertyInfo.ShouldNotBeNull();
            propertyInfo.CanWrite.ShouldBeFalse();

        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task Create_Model_Assembly_in_path_if_not_Exist(Platform platform){
            InitializeMapperService(nameof(Create_Model_Assembly_in_path_if_not_Exist),platform);
            var typeToMap = typeof(TestModelMapper);

            typeToMap.MapToModel();
            var mapToModel = await ModelMapperService.MappedTypes;

            File.Exists(mapToModel.Assembly.Location).ShouldBeTrue();
        }

        [Fact]
        public async Task Do_Not_Map_If_Type_Assembly_Version_Not_Changed(){
            InitializeMapperService(nameof(Do_Not_Map_If_Type_Assembly_Version_Not_Changed));
            var mappedType = typeof(TestModelMapper);

            mappedType.MapToModel();
            var mapToModel = await ModelMapperService.MappedTypes;

            var modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperServiceAttribute),false)
                .OfType<ModelMapperServiceAttribute>().FirstOrDefault(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.ShouldNotBeNull();

            var version = modelMapperAttribute.Version;

            mappedType.MapToModel();
            mapToModel = await ModelMapperService.MappedTypes;

            modelMapperAttribute = mapToModel.Assembly.GetCustomAttributes(typeof(ModelMapperServiceAttribute),false)
                .OfType<ModelMapperServiceAttribute>().First(attribute => attribute.MappedType==mappedType.FullName&&attribute.MappedAssemmbly==mappedType.Assembly.GetName().Name);
            modelMapperAttribute.Version.ShouldBe(version);
        }

        [Fact]
        public async Task Always_Map_If_ModelMapperModule_Version_Changed(){
            
            InitializeMapperService(nameof(Always_Map_If_ModelMapperModule_Version_Changed));
            var mappedType = typeof(TestModelMapper);
            mappedType.MapToModel();
            await ModelMapperService.MappedTypes;

            typeof(ModelMapperService).SetFieldValue("_modelMapperModuleVersion", new Version(2000,100,40));

            var exception = Should.Throw<Exception>(async () => {
                mappedType.MapToModel();
                await ModelMapperService.MappedTypes;
            });

            exception.Message.ShouldStartWith("error CS0016: Could not write to output file");
        }

    }

}