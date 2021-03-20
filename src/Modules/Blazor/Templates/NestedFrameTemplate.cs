using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Templates.ActionControls;
using Fasterflect;

namespace Xpand.XAF.Modules.Blazor.Templates {
    public class NestedFrameTemplate:DevExpress.ExpressApp.Blazor.Templates.NestedFrameTemplate {
        protected override IEnumerable<IActionControlContainer> CreateActionControlContainers() {
            return base.CreateActionControlContainers();
            // this.SetPropertyValue(nameof(ViewActionContainers),new List<IActionControlContainer>(ViewActionContainers)) ;
            // this.SetPropertyValue(nameof(SelectionDependentActionContainers),new List<IActionControlContainer>(ViewActionContainers)) ;
            // this.SetPropertyValue(nameof(SelectionIndependentActionContainers),new List<IActionControlContainer>(ViewActionContainers)) ;
            // return ViewActionContainers.Concat(SelectionDependentActionContainers).Concat(SelectionIndependentActionContainers).ToArray();
        }
    }
}