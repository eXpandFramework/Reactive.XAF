using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static partial class AttributesExtensions {
        internal static IObservable<Unit> Attributes(this ApplicationModulesManager manager)
            => manager.XpoAttributes()
                .Merge(manager.WhenCustomizeTypesInfo()
                    .InvisibleInAllViewsAttribute()
                    .InvisibleInAllListViewsAttribute()
                    .CustomAttributes()
                    .MapTypeMembersAttributes()
                    .VisibleInAllViewsAttribute()
                    .ToUnit())
                .Merge(manager.ListViewShowFooterCollection())
                .Merge(manager.VisibleInAllViewsAttribute())
                .Merge(manager.ColumnSummary())
                .Merge(manager.EditorAliasDisabledInDetailViewAttribute())
                .Merge(manager.ColumnSorting())
                .Merge(manager.DisableNewObjectAction())
                .Merge(manager.HiddenActions())
                .Merge(manager.ReadOnlyCollection())
                .Merge(manager.ReadOnlyProperty())
                .Merge(manager.QuickAccessNavigationItemActions())
                .Merge(manager.LookupPropertyAttribute())
                .Merge(manager.LinkUnlinkPropertyAttribute())
                .Merge(manager.ReadOnlyObjectViewAttribute())
                .Merge(manager.DetailCollectionAttribute())
                .Merge(manager.AppearanceToolAttribute())
                .Merge(PreventAggregatedObjectsValidationAttribute(manager))
        ;
    }

    

    
}