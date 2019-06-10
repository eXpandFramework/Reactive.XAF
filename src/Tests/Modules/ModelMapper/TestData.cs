using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Tests.Modules.ModelMapper{
    public class TestModelMapper{
        public int Age{ get; set; }
        
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
        [PrivateAttribute]
        public string PrivateAttribute{ get; set; }
    }

    class PrivateDescription:DescriptionAttribute{
        

        public override string Description =>GetType().Name ;
    }
    class PrivateDescriptionAttributesClass{
        [PrivateDescription]
        public string PrivateAttribute{ get; set; }
    }
    internal class CopyAttributesClass{
        [Description][PrivateAttribute]
        public string AttributeNoParam{ get; set; }
        [PrivateAttribute]
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

    
    internal class ResevredProperties : IModelNode{
        public IEnumerable<string> Strings{ get; } = new List<string>();

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