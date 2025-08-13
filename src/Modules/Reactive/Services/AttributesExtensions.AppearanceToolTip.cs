using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.Attributes.Appearance;
using Xpand.Extensions.XAF.ModelExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public class ToolTipAppearanceRule(IAppearanceRuleProperties properties, IObjectSpace objectSpace)
        : AppearanceRule(properties, objectSpace) {
        protected override IList<IConditionalAppearanceItem> CreateAppearanceItems(AppearanceState state) {
            var result = base.CreateAppearanceItems(state);
            if(Properties is IModelAppearanceWithToolTipRule { ToolTip: not null } rule) {
                result.Add(new AppearanceItemToolTip(state, Properties.Priority, rule.ToolTip,rule.Id()));
            }
            return result;
        }
    }
        
    public interface IModelAppearanceWithToolTipRule:IModelNode {
        string ToolTip { get; set; }
    }
        
    [DomainLogic(typeof(IModelAppearanceWithToolTipRule))]
    public static class ModelAppearanceRuleLogic {
        public static string Get_ToolTip(IModelAppearanceWithToolTipRule modelAppearanceRule) 
            => ((IModelAppearanceRule)modelAppearanceRule).Attribute is AppearanceToolTipAttribute ? ((AppearanceToolTipAttribute)((IModelAppearanceRule)modelAppearanceRule).Attribute).ToolTip : "";
    }

    public class AppearanceItemToolTip : AppearanceItemBase {
        public string ID{ get; }

        public AppearanceItemToolTip(AppearanceState state, int priority, string toolTip, string id)
            : base(state, priority) {
            ID = id;
            if(state == AppearanceState.CustomValue) {
                ToolTipText = toolTip;
            }
        }
        public string ToolTipText { get; }
        protected override void ApplyCore(object targetItem) {}
    }}