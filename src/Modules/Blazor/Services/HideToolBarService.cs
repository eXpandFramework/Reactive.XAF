using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Templates;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services {
    internal static class HideToolBarService {
        internal static IObservable<Unit> HideToolBarConnect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenNestedFrameCreated()
                .SelectMany(frame => frame.WhenTemplateChanged()
                    .WhenToolBarNeedHide()
                    .Do(template => {
                        // ((List<IActionControlContainer>) template.ViewActionContainers).Clear();
                        // ((List<IActionControlContainer>) template.SelectionIndependentActionContainers).Clear();
                        // ((List<IActionControlContainer>) template.SelectionDependentActionContainers).Clear();
                    }))).ToUnit();

        private static IObservable<NestedFrameTemplate> WhenToolBarNeedHide(this IObservable<Frame> source)
            => source.Where(nestedFrame => {
                    var modelListView = nestedFrame.View.AsListView().Model;
                    if (!modelListView.HasValue("HideToolBar")) return false;
                    var value = modelListView.GetValue<bool?>("HideToolBar");
                    return value != null && value.Value;
                })
                .Select(nestedFrame => nestedFrame.Template)
                .Cast<NestedFrameTemplate>();

    }
}
