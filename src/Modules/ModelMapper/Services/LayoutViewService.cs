using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ModelMapper.Services{

    internal static class LayoutViewService{
        public static IObservable<Unit> ConnectLayoutView(this ApplicationModulesManager applicationModulesManager){
//            applicationModulesManager.ExtendMap(PredifinedMap.LayoutView)
            return Observable.Empty<Unit>();
            ;
            var extendModel = applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(module => module.ExtendModel);
            return extendModel.Select(extenders => {
                var layoutViewExtenderType = extenders.GetInterfaceExtenders(typeof(IModelListView))
                    .Where(_ => typeof(IModelModelMapContainer).IsAssignableFrom(_))
                    .FirstOrDefault(type => type.Attribute<ModelMapLinkAttribute>().LinkedTypeName.StartsWith(PredifinedMap.LayoutView.GetTypeName()));
                if (layoutViewExtenderType != null){
                    var mapType = layoutViewExtenderType.Properties().First(_ => typeof(IModelModelMap).IsAssignableFrom(_.PropertyType)).PropertyType;
                    extenders.Add(mapType,typeof(IModelDesignLayoutView));
                }

                return Unit.Default;
            });
        }
    }
}