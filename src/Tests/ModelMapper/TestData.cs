using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Security.Principal;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Utils;

namespace Xpand.XAF.Modules.ModelMapper.Tests{
    public class TestModelMapper{
        public int Age{ get; set; }
        
    }
    public class TestModelMapperCommonType1{
        public int Age{ get; set; }
        public TestModelMapperCommon TestModelMapperCommon{ get;  }    =new TestModelMapperCommon();
        public AppearanceObject AppearanceCell{ get; } = new AppearanceObjectEx();
    }
    public class TestModelMapperCommonType2{
        public int Age{ get; set; }
        public AppearanceObjectEx AppearanceCell{ get; } = new AppearanceObjectEx();
        public TestModelMapperCommon TestModelMapperCommon{ get;  }=new TestModelMapperCommon();
        
    }

    public class TestModelMapperCommon{
        public string Test{ get; set; }
    }

    public class StringValueTypeProperties{
        public int WIntegerField;
        public string WStringField;

        public StringValueTypeProperties(){
            RInteger = 0;
            RString = null;
        }

        public Size Size{ get; set; }
        public int? NullAbleRWInteger{ get; set; }
        public int RWInteger{ get; set; }
        public int RInteger{ get; }

        public int WInteger{
            set => WIntegerField = value;
        }

        public string RWString{ get; set; }
        public string RString{ get; }

        public string WString{
            set => WStringField = value;
        }

        public ValueTypeContainer.NestedEnum NestedEnumProperty{ get; set; }
    }

    public class ValueTypeContainer{
        public enum NestedEnum{
        }
    }

    class PrivateAttribute:Attribute{
        
    }
    public class MyClassAttribute : Attribute{
        public MyClassAttribute(string s1, string s2){
            S1 = s1;
            S2 = s2;
        }

        public MyClassAttribute(LayoutColumnPosition layoutColumnPosition){
            LayoutColumnPosition = layoutColumnPosition;
        }

        public LayoutColumnPosition LayoutColumnPosition{ get; }
        public string S1{ get; }

        public string S2{ get; }
    }

    class ReplaceAttributesClass{
        [Private]
        public string PrivateAttribute{ get; set; }
    }

    class PrivateDescription:DescriptionAttribute{
        public PrivateDescription(string description) : base(description){
        }

        
    }

    public class PrivateDescriptionAttributesClass{
        public const string Description = "Private Description AttributesClass";
        [PrivateDescription(Description)]
        public string PrivateAttribute{ get; set; }
    }

    public class DefaultValueAttributesClass{
        [DefaultValue(null)]
        public string PrivateAttribute{ get; set; }
    }

    public class FlagAttributesClass{
        [FlagParameter(FlagEnum.Val2|FlagEnum.Val3)]
        public string FlagPropertyValue{ get; set; }
        [FlagParameter(FlagEnum.Val2)]
        public string FlagProperty{ get; set; }
    }

    public class FlagParameterAttribute:Attribute{
        public FlagParameterAttribute(FlagEnum flagEnum){
            FlagEnum = flagEnum;
        }

        public FlagEnum FlagEnum{ get; }
    }

    [Flags]
    public enum FlagEnum{
        Val1=0,
        Val2=1,
        Val3=2
    }

    class EscapeAttributeString{
        public const string Description = @"test with ""quotes""";
        [Description(Description)]
        public string Property{ get; set; }
    }
    internal class CopyAttributesClass{
        [Description][Private]
        public string AttributeNoParam{ get; set; }
        [Private]
        public string AttributePrivate{ get; set; }

        [Index(1)]
        public string AttributeValueTypeParam{ get; set; }
        [DefaultValue(false)]
        public string AttributeDefaultVvalueAttribue{ get; set; }
        

        [Description("test")]
        public string AttributeStringParam{ get; set; }

        [MyClass("t1", "t2")]
        public string AttributeTwoParam{ get; set; }

        [TypeConverter(typeof(string))]
        public string AttributeTypeParam{ get; set; }
        
        [MyClass(LayoutColumnPosition.Left)]
        public string AttributeEnumParam{ get; set; }
    }

    class NestedTypeProperties{
        public NestedTypeProperty Property{ get; set; }

        public class NestedTypeProperty{
        }

    }

    class BaseTypeProperties:BaseTypePropertiesBase{
        

        public class NestedTypeProperty{
        }

    }

    class BaseTypePropertiesBase{
        public string Test{ get; set; }
        public AppearanceObjectEx AppearanceCell{ get; } = new AppearanceObjectEx();
    }
    class NonBrowsableProperties{
        public string Test{ get; set; }
        [Browsable(false)]
        public string NonBroswsableTest{ get; set; }
        
    }

    internal class CollectionProperties {
        public IEnumerable<string> Strings{ get; } = new List<string>();
        public IEnumerable<TestModelMapper> Tests{ get; } = new List<TestModelMapper>();
        public IdentityReferenceCollection IdentityReferenceCollection{ get; } = new IdentityReferenceCollection();
        
    }
    internal class DXDesignTimeAttributeClass {
        [TypeConverter("DevExpress.XtraGrid.TypeConverters.FieldNameTypeConverter, DevExpress.XtraGrid.v19.1.Design")]
        public string Test{ get; set; }
    }

    internal class ResevredProperties : IModelNode{
        public string Item{ get; set; }
        public bool IsReadOnly{ get; set; }

        public IModelNode GetNode(int index){
            throw new NotImplementedException();
        }

        public IModelNode GetNode(string id){
            throw new NotImplementedException();
        }

        public TNodeType AddNode<TNodeType>(){
            throw new NotImplementedException();
        }

        public TNodeType AddNode<TNodeType>(string id){
            throw new NotImplementedException();
        }

        public void Remove(){
            throw new NotImplementedException();
        }

        public TValueType GetValue<TValueType>(string name){
            throw new NotImplementedException();
        }

        public void SetValue<TValueType>(string name, TValueType value){
            throw new NotImplementedException();
        }

        public void ClearValue(string name){
            throw new NotImplementedException();
        }

        public bool HasValue(string name){
            throw new NotImplementedException();
        }

        public IModelNode Parent{ get; set; }
        public IModelNode Root{ get; set; }
        public int? Index{ get; set; }
        public int NodeCount{ get; set; }
        public IModelApplication Application{ get; set; }
    }

    internal class ReferenceTypeProperties{
        public ReferenceTypeProperties(){
            RStringValueTypeProperties = new StringValueTypeProperties();
        }

        public StringValueTypeProperties RWStringValueTypeProperties{ get; set; }
        public StringValueTypeProperties RStringValueTypeProperties{ get; }
    }

    public class NestedSelfReferenceTypeProperties{
        public NestedSelfReferenceTypeProperties(SelfReferenceTypeProperties selfReferenceTypeProperties){
            SelfReferenceTypeProperties = selfReferenceTypeProperties;
        }

        public SelfReferenceTypeProperties SelfReferenceTypeProperties{ get; }
    }
    public class SelfReferenceTypeProperties{
        public NestedSelfReferenceTypeProperties NestedSelfReferenceTypeProperties{ get; set; }
        public SelfReferenceTypeProperties Self{ get; set; } 
    }
}