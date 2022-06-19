// [assembly:
//     Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapperServiceAttribute(
//         "a8ce711e-890f-e077-3875-bc3052ee8f7f")]
//
// [assembly:
//     Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapperTypeAttribute(
//         "Xpand.XAF.Modules.ModelMapper.Tests.RootType", "Xpand.XAF.Modules.ModelMapper.Tests",
//         "3b483ffa-9c99-4b76-b5e4-45eeff0c575c", "67a3a318-e4e7-e981-d14a-b64886997ee7")]
// [assembly: System.Reflection.AssemblyVersionAttribute("4.212.9.8")]
// [assembly: System.Reflection.AssemblyFileVersionAttribute("4.212.9.8")]
//
// [DevExpress.ExpressApp.DC.DomainLogicAttribute(typeof(IModelRootTypeMapModelMappers))]
// public class IModelRootTypeMapModelMappersDomainLogic {
//     public static int? Get_Index(IModelRootTypeMapModelMappers mapper) {
//         return 0;
//     }
// }
//
// [DevExpress.ExpressApp.Model.ModelDisplayNameAttribute("RootType")]
// public interface IModelXpandXAFModulesModelMapperTests_RootType : Xpand.XAF.Modules.ModelMapper.IModelModelMap {
//     IModelXpandXAFModulesModelMapperTests_NestedType NestedType { get; }
//
//     System.String Value { get; set; }
//
//     IModelXpandXAFModulesModelMapperTests_TestModelMapper RootTestModelMapper { get; }
//     new int? Index { get; set; }
//     IModelRootTypeMapModelMappers ModelMappers { get; }
// }
//
// [Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.ModelMapLinkAttribute(
//     "Xpand.XAF.Modules.ModelMapper.Tests.RootType, Xpand.XAF.Modules.ModelMapper.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=c52ffed5d5ff0958")]
// public interface IModelRootTypeMap : Xpand.XAF.Modules.ModelMapper.IModelModelMapContainer {
//     [Xpand.XAF.Modules.ModelMapper.ModelMapperBrowsableAttribute(
//         typeof(Xpand.XAF.Modules.ModelMapper.ModelMapperVisibilityCalculator), null)]
//     IModelXpandXAFModulesModelMapperTests_RootType RootType { get; }
// }
//
// [System.ComponentModel.DescriptionAttribute(
//     "These mappers relate to Application.ModelMapper.MapperContexts and applied first.")]
// [DevExpress.ExpressApp.Model.ModelNodesGeneratorAttribute(
//     typeof(Xpand.XAF.Modules.ModelMapper.ModelMapperContextNodeGenerator))]
// [DevExpress.Persistent.Base.ImageNameAttribute("Context_Menu_Show_In_Popup")]
// public interface IModelRootTypeMapModelMappers :
//     DevExpress.ExpressApp.Model.IModelList<Xpand.XAF.Modules.ModelMapper.IModelMapperContextContainer>,
//     DevExpress.ExpressApp.Model.IModelNode { }
//
//
// public interface IModelXpandXAFModulesModelMapperTests_NestedType : Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled {
//     IModelXpandXAFModulesModelMapperTests_TestModelMapper NestedTestModelMapper { get; }
//
//     IModelXpandXAFModulesModelMapperTests_NestedType2 NestedType2 { get; }
// }
//
//
// public interface IModelXpandXAFModulesModelMapperTests_NestedType2 : Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled {
//     IModelXpandXAFModulesModelMapperTests_NestedType NestedType { get; }
//
//     IModelXpandXAFModulesModelMapperTests_TestModelMapper Nested2TestModelMapper { get; }
// }
//
//
// public interface
//     IModelXpandXAFModulesModelMapperTests_TestModelMapper : Xpand.XAF.Modules.ModelMapper.IModelNodeDisabled {
//     System.String Name { get; set; }
// }