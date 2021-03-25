using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.Collections;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.XAF.Modules.Reactive.Objects;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    internal static class RestPropertyExtensions {
        public static IObservable<ApplicationModulesManager> RestPropertyTypes(this IObservable<ApplicationModulesManager> source)
            => source.MergeIgnored(manager => manager.WhenCustomizeTypesInfo()
                .SelectMany(t => {
                    var concatIgnored = t.e.TypesInfo.PersistentTypes.ToObservable(Scheduler.Immediate)
                        .MarkListMembers()
                        .MarkReactiveCollectionMembers()
                        .MarkNonBrowsableMembers()
                        ;
                    return concatIgnored;
                })
                
                    );

        private static IObservable<ITypeInfo> MarkNonBrowsableMembers(this IObservable<ITypeInfo> source)
            => source.ConcatIgnored(info => new[] {typeof(Dictionary<string, string>), typeof(object)}.ToObservable()
                .SelectMany(type => info.Members.Where(memberInfo => memberInfo.MemberType == type).Execute(memberInfo =>
                        memberInfo.AddAttribute(new BrowsableAttribute(false)))));

        private static IObservable<ITypeInfo> MarkReactiveCollectionMembers(this IObservable<ITypeInfo> source)
            => source.ConcatIgnored(info => info.Type.ReactiveCollectionMembers()
                .Do(t1 => t1.info.AddAttribute(new ReadOnlyCollectionAttribute(disableListViewProcess:true))));

        private static IObservable<ITypeInfo> MarkListMembers(this IObservable<ITypeInfo> source) 
            => source.ConcatIgnored(info => info.Type.RestListMembers()
                .Do(t1 => {
                    t1.info.AddAttribute(new BrowsableAttribute(false));
                    t1.info.Owner.FindMember(t1.attribute.PropertyName)
                        .AddAttribute(new CollectionOperationSetAttribute()
                            {AllowAdd = true, AllowRemove = true});
                }));

        public static IObservable<object> RestPropertyDependent(this IObservable<object[]> source,IObjectSpace objectSpace,Type objectType,ICredentialBearer bearer)
            => source.ConcatIgnored(objects => objectSpace.RestPropertyDependentName(objectType, objects))
                .ConcatIgnored(objects => objects.RestPropertyDependentReadOnly(objectType,bearer))
            ;

        private static IObservable<object> RestPropertyDependentReadOnly(this object[] objects,Type objectTpe,ICredentialBearer bearer) 
            =>objectTpe.RestDependentMembersByReadOnly()
                .SelectMany(t => objects.ToObservable(Scheduler.Immediate)
                    .SelectMany(o => t.attribute.Send(t.info.MemberType.CreateInstance(),bearer,t.attribute.RequestUrl(o))
                        .Do(o1 => t.info.SetValue(o,o1))));

        private static IObservable<object> RestPropertyDependentName(this IObjectSpace objectSpace,Type objectType, object[] objects) {
            return objectType.RestDependentMembersByName()
                .SelectMany(t => objectSpace.RequestAll(t.info.Owner.FindMember(t.attribute.PropertyName).MemberType)
                    .SelectMany(dependedObjects => objects.ToObservable()
                        .Do(sourceObject => {
                            var dependedObject = dependedObjects.FirstOrDefault(o1 =>
                                o1.GetTypeInfo().KeyMember.GetValue(o1).Equals(t.info.GetValue(sourceObject)));
                            if (dependedObject != null) {
                                t.info.Owner.FindMember(t.attribute.PropertyName).SetValue(sourceObject, dependedObject);
                            }
                        })));
        }

        public static IObservable<object> RestPropertyBindingListsInit(this IObservable<object> source, IObjectSpace objectSpace)
            => source.ConcatIgnored(o => o.GetType().RestListMembers()
                .WhenDefault(_ => objectSpace.IsDisposed)
                .Do(t => {
                    var realType = t.info.MemberType.RealType();
                    var targetMember = t.info.Owner.FindMember(t.attribute.PropertyName);
                    var objects = ((IEnumerable) t.info.GetValue(o)).Cast<object>();
                    var bindingList = (realType.IsValueType || realType == typeof(string)
                        ? objects.ToObjectString(objectSpace)
                        : objects.ToBindingList(realType));
                    targetMember.SetValue(o, bindingList);
                }));

        public static IObservable<object> RestPropertyDependentChange(this IObservable<object> source)
            => source.OfType<INotifyPropertyChanged>().MergeIgnored(o => o.WhenPropertyChanged()
                .SelectMany(t => o.GetType().RestDependentMembersByName().Where(t1 => t1.attribute.PropertyName==t.e.PropertyName)
                    .Do(t3 => {
                        var dependentValue = t3.info.Owner.FindMember(t.e.PropertyName).GetValue(t.sender);
                        t3.info.SetValue(t.sender,dependentValue.GetTypeInfo().KeyMember.GetValue(dependentValue));
                    })));

        private static IObservable<object> BindingListsDataSource(this object source, XafApplication xafApplication) {
            var detailView = xafApplication.WhenDetailViewCreated(typeof(ObjectString)).Select(t => t.e.View)
                .Where(view => view.Items.OfType<PropertyEditor>().Any(item => item.MemberInfo.MemberType==typeof(ObjectString)))
                .Publish().RefCount();
            return source.GetType().RestListMembers()
                .Select(t => t.info.Owner.FindMember(t.attribute.PropertyName))
                .SelectMany(member => {
                    if (member.MemberType.RealType() == typeof(ObjectString)) {
                        var objectStrings = (((IEnumerable) member.GetValue(source))).Cast<ObjectString>()
                            .ToObservable(Scheduler.Immediate);
                        // var dataSourceProperty =
                        //     member.FindAttribute<DataSourcePropertyAttribute>()?.DataSourceProperty;
                        //
                        // if (dataSourceProperty != null) {
                        //     var dynamicCollection =
                        //         (DynamicCollection) member.Owner.FindMember(dataSourceProperty).GetValue(source);
                        //     objectStrings = objectStrings.Do(s => s.SetDatasource(dynamicCollection))
                        //
                        //         .Merge(detailView
                        //             .Do(view => ((ObjectString) view.CurrentObject).SetDatasource(dynamicCollection)).OfType<ObjectString>());
                        // }

                        return objectStrings
                            .SelectMany(objectString =>
                                objectString.WhenCheckedListBoxItems(member, source).To(source));
                    }

                    return Observable.Empty<object>();
                });
        }

        public static IObservable<object> RestPropertyBindingListsDataSource(this IObservable<object> source,
            IObjectSpace objectSpace, XafApplication xafApplication)
            => source.MergeIgnored(o => objectSpace.WhenNewObjectCreated<IObjectSpaceLink>().StartWith(o)
                .SelectMany(o1 => o1.BindingListsDataSource(xafApplication)) );

        public static IObservable<object> RestPropertyBindingListsChange(this IObservable<object> source)
            => source.MergeIgnored(o => o.GetType().RestListMembers()
                .SelectMany(t => ((IBindingList) ((IEnumerable) t.info.Owner.FindMember(t.attribute.PropertyName).GetValue(o))).WhenListChanged().To(t))
                .Do(t => {
                    var realType = t.info.MemberType.RealType();
                    var sourceMember = t.info.Owner.FindMember(t.attribute.PropertyName);
                    var defaultValues = sourceMember.ToDefaultValues(o);
                    if (!(realType == typeof(string) || realType.IsValueType)) {
                        defaultValues = ((IEnumerable) sourceMember.GetValue(o)).Cast<string>().ToArray();
                    }
                    t.info.SetValue(o, defaultValues);
                }));

        public static IObservable<object> ReactiveCollectionsInit(this IObservable<object> source,IObjectSpace objectSpace)
            => source.ConcatIgnored(o => o.GetType().ReactiveCollectionMembers()
                .Do(t => t.info.SetValue(o, t.info.MemberType.CreateInstance(objectSpace))));

        public static IObservable<object> ReactiveCollectionsFetch(this IObservable<object> source,ICredentialBearer bearer)
            => source.MergeIgnored(o => o.GetType().ReactiveCollectionMembers()
                .SelectMany(t => {
                    var dynamicCollection = ((DynamicCollection) t.info.GetValue(o));
                    return dynamicCollection.WhenFetchObjects()
                        
                        
                        .SelectMany(t2 => {
                            var realType = t.info.MemberType.RealType();
                            return t.attribute.Send(realType.CreateInstance(), bearer, t.attribute.RequestUrl(o),
                                        realType.DeserializeResponse(t2.sender.ObjectSpace))
                                    .Do(o1 => {},() => {})
                                    
                                    .BufferUntilCompleted()

                                    .Do(objects => t2.sender.AddObjects(objects, true))
                                ;
                        });
                }));

        private static Func<string, object[]> DeserializeResponse(this Type realType, IObjectSpace objectSpace) 
            => s => realType != typeof(ObjectString) ? realType.Deserialize<object>(s) : typeof(string).Deserialize<string>(s)
                .ToObjectString(objectSpace).Cast<object>().ToArray();
    }
}