using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.Attributes{
    [AttributeUsage(AttributeTargets.Property)]
    public class ShowViewParametersAttribute:Attribute {
        public ShowViewParametersAttribute(TargetWindow targetWindow) => TargetWindow=targetWindow;

        public ShowViewParametersAttribute(NewWindowTarget newWindowTarget) => NewWindowTarget=newWindowTarget;

        public NewWindowTarget? NewWindowTarget { get; }
        public TargetWindow? TargetWindow { get; }
    }
}