using System;
using DevExpress.ExpressApp.ConditionalAppearance;

namespace Xpand.Extensions.XAF.Attributes.Appearance{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class AppearanceToolTipAttribute : AppearanceAttribute {
        public AppearanceToolTipAttribute(string id) : base(id) { }

        public AppearanceToolTipAttribute(string id, string criteria) : base(id, criteria) => ToolTip = criteria;

        public AppearanceToolTipAttribute(string id, AppearanceItemType appearanceItemType, string criteria) : base(id,
            appearanceItemType, criteria)
            => ToolTip = criteria;

        public string ToolTip { get; set; }
    }

    
}