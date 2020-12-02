using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;

namespace Xpand.TestsLib.Common{
    public class CustomPropertyEditor : PropertyEditor{
        private readonly object _control;

        public CustomPropertyEditor(Type objectType, IModelMemberViewItem model, object control) : base(objectType,
            model){
            _control = control;
        }

        protected override object CreateControlCore(){
            return _control;
        }

        protected override void ReadValueCore(){
        }

        protected override object GetControlValueCore(){
            return _control;
        }
    }
}