using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    public static class RestTypeExtensions {
        public static string[] ToDefaultValues(this IMemberInfo targetMember, object o) 
            => ((IEnumerable) targetMember.GetValue(o)).Cast<object>()
                .ToDefaultValues(targetMember.MemberTypeInfo.Type.RealType().ToTypeInfo()).ToArray();

        public static IObservable<(IMemberInfo info, RestPropertyAttribute attribute)> RestListMembers(this Type type)
            => type.RestProperties().Where(t => t.attribute.PropertyName!=null).Where(t => t.info.IsList);
        
        public static IObservable<(IMemberInfo info, RestPropertyAttribute attribute)> RestDependentMembersByName(this Type type)
            => type.RestProperties().Where(t => (t.attribute.PropertyName != null)).Where(t => !t.info.IsList);
        
        public static IObservable<(IMemberInfo info, RestPropertyAttribute attribute)> RestDependentMembersByReadOnly(this Type type)
            => type.RestProperties().Where(t => (t.attribute.PropertyName == null&&t.info.IsReadOnly)).Where(t => !t.info.IsList);

        public static IObservable<(IMemberInfo info, RestPropertyAttribute attribute)> ReactiveCollectionMembers(this Type type) 
            => type.RestProperties().Where(t => t.attribute.RequestUrl != null && t.info.MemberType.IsAssignableToGenericType(typeof(ReactiveCollection<>)));

        public static IObservable<(IMemberInfo info, RestPropertyAttribute attribute)> RestProperties(this Type type) 
            => type.ToTypeInfo().OwnMembers.SelectMany(info => info.FindAttributes<RestPropertyAttribute>()
                .Select(attribute => (info,attribute))).ToArray().ToObservable(Scheduler.Immediate);

        public static (RestActionOperationAttribute attribute, ITypeInfo info)[] OperationActionTypes(this IEnumerable<ITypeInfo> types) 
            => types.SelectMany(info => info.FindAttributes<RestActionOperationAttribute>()
                .Select(attribute => (attribute, info))).ToArray();

        public static IObservable<(object o1, IMemberInfo target)> RestPropertiesDefault(this IObjectSpace objectSpace, object o) 
            => o.GetType().RestProperties().Where(t => t.attribute.PropertyName!=null)
                .SelectMany(t => objectSpace.Get(t.info.MemberType)
                    .Select(o1 => (o1, target: t.info, source: t.info.Owner.FindMember(t.attribute.PropertyName).GetValue(o), key: t.info.Owner.KeyMember))
                    .Where(t1 => t1.source.Equals(t1.o1.GetTypeInfo().KeyMember.GetValue(t1.o1)))
                    .Select(t1 => (t1.o1, t1.target)));
    }
}