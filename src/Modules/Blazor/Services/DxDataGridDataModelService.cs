using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors.Grid;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Blazor.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services {
    public static class DxDataGridDataModelService {
        internal static IObservable<Unit> ApplyDxDataGridModel(this XafApplication application) 
            => application.WhenFrameViewControls().WhenFrame(viewType: ViewType.ListView)
                .DxDataGridModel().Apply();

        private static IEnumerable<(Frame frame, IModelListViewFeature feature)> FrameFeatures(this Frame frame) 
            => frame.Application.Model.ToReactiveModule<IModelReactiveModulesBlazor>().Blazor.ListViewFeatures.Where(feature => feature.ListView==frame.View.Model)
                .Select(feature => (frame,feature));

        private static IObservable<Unit> Apply(this IObservable<(Frame frame, IModelListViewFeature feature)> source) {
            var propertyInfos=new HashSet<string>(typeof(IModelDxDataGridModel).GetProperties().Select(info => info.Name));
            return source.SelectMany(t => {
                    var dxDataGridModel = ((GridListEditor) t.frame.View.AsListView().Editor).GetDataGridAdapter().DataGridModel;
                    return propertyInfos.Select(s => (name:s,value:t.feature.DxDataGridModel.GetValue(s))).Where(t3 => t3.value!=null)
                        .Select(t1 =>( t1.name,t1.value,t.frame))
                        .Do(t2 => dxDataGridModel.SetPropertyValue(t2.name, t2.value));
                })
                
                .ToUnit();
        }

        private static IObservable<(Frame frame, IModelListViewFeature feature)> DxDataGridModel(this IObservable<Frame> source) 
            => source.SelectMany(frame => frame
                .FrameFeatures().Where(t => (t.feature.IsPropertyVisible(nameof(IModelListViewFeature.DxDataGridModel))))
                .Select(t => (frame,t.feature)));
    }
}