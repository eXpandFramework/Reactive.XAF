

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
public static partial class AttributesExtensions {
        static IObservable<Unit> XpoAttributes(this ApplicationModulesManager manager)
            => manager.WhenCustomizeTypesInfo().Take(1).Select(e => e.TypesInfo)
                .DoItemResilient(typesInfo => AppDomain.CurrentDomain.GetAssemblyType("Xpand.Extensions.XAF.Xpo.XpoExtensions")
                    ?.Method("CustomizeTypesInfo",Flags.StaticAnyVisibility).Call(null,typesInfo))
                .ToUnit();

        static IObservable<Unit> VisibleInAllViewsAttribute(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.Members<VisibleInAllViewsAttribute>().ToArray().ToNowObservable()
                    .SelectManyItemResilient(t1 => new Attribute[] { new VisibleInDetailViewAttribute(true), new VisibleInListViewAttribute(true), new VisibleInLookupListViewAttribute(true) }
                        .Execute(attribute => t1.info.AddAttribute(attribute))))
                .ToUnit();
                
        static IObservable<CustomizeTypesInfoEventArgs> InvisibleInAllViewsAttribute(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.Members<InvisibleInAllViewsAttribute>().ToArray().Observe()
                .SelectManyItemResilient(attributes => attributes.AddVisibleViewAttributes()
                    .Concat(attributes.Distinct(t1 => t1.info).ToArray().AddAppearanceAttributes())));

        private static IEnumerable<Attribute> AddVisibleViewAttributes(this (InvisibleInAllViewsAttribute attribute, IMemberInfo info)[] source) 
            => source.Where(t => t.attribute.Layer == OperationLayer.Model)
                .SelectMany(t => new Attribute[] {
                    new VisibleInDetailViewAttribute(false), new VisibleInListViewAttribute(false),
                    new VisibleInLookupListViewAttribute(false)
                }.Execute(attribute => t.info.AddAttribute(attribute)));
        
        private static IEnumerable<Attribute> AddAppearanceAttributes(this (InvisibleInAllViewsAttribute attribute, IMemberInfo info)[] source) 
            => source.Where(t => t.attribute.Layer == OperationLayer.Appearance)
                .SelectMany(t => new Attribute[] {
                    new AppearanceAttribute($"Hide {t.info}",AppearanceItemType.ViewItem, "1=1") {
                        TargetItems = t.info.Name,Visibility = ViewItemVisibility.ShowEmptySpace
                    }
                }.Execute(attribute => t.info.Owner.AddAttribute(attribute)));

        static IObservable<CustomizeTypesInfoEventArgs> InvisibleInAllListViewsAttribute(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.Members<InvisibleInAllListViewsAttribute>().ToArray().ToNowObservable()
                    .SelectManyItemResilient(t1 => new Attribute[] {
                        new VisibleInListViewAttribute(false),
                        new VisibleInLookupListViewAttribute(false)
                    }.Execute(attribute => t1.info.AddAttribute(attribute))));

        static IObservable<CustomizeTypesInfoEventArgs> MapTypeMembersAttributes(this IObservable<CustomizeTypesInfoEventArgs> source)
            => source.ConcatIgnored(e => e.TypesInfo.PersistentTypes.ToNowObservable()
                .SelectManyItemResilient(info => info.FindAttributes<MapTypeMembersAttribute>()
                    .SelectMany(attribute => attribute.Source.ToTypeInfo().OwnMembers)
                    .WhereDefault(memberInfo => info.FindMember(memberInfo.Name))
                    .Execute(memberInfo => info.CreateMember(memberInfo.Name, memberInfo.MemberType)).IgnoreElements()
                    .ToArray().Finally(() => XafTypesInfo.Instance.RefreshInfo(info))
                    .ToNowObservable()));
        
        static IObservable<CustomizeTypesInfoEventArgs> CustomAttributes(this IObservable<CustomizeTypesInfoEventArgs> source) 
            => source.ConcatIgnored(e => e.TypesInfo.PersistentTypes.ToNowObservable()
                .SelectManyItemResilient(info => info.Members.SelectMany(memberInfo => memberInfo.FindAttributes<Attribute>()
                    .OfType<ICustomAttribute>().ToArray().Select(memberInfo.AddCustomAttribute))
                ).Concat(e.TypesInfo.PersistentTypes.ToNowObservable().SelectManyItemResilient(typeInfo => typeInfo
                    .FindAttributes<Attribute>().OfType<ICustomAttribute>().ToArray().Select(typeInfo.AddCustomAttribute)))
                
                );
    }}