using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ModelService {
        public static IObservable<Unit> LookupPropertyAttribute(this ApplicationModulesManager  manager) 
            => manager.WhenGeneratingModelNodes<IModelBOModel>()
                .SelectMany(modelClass => modelClass)
                .SelectMany(mc => mc.OwnMembers.SelectMany(member => member.MemberInfo
                    .FindAttributes<ModelLookupPropertyAttribute>()
                    .Do(attribute => member.LookupProperty = attribute.Property)))
                .ToUnit();
    }
}