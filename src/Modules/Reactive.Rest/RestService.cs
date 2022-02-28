using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Filtering;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Rest.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Rest {

    public static class RestService {
        public static readonly ConcurrentDictionary<object, IConnectableObservable<object>> CacheStorage = new();
        public static HttpClient HttpClient {
            get => Xpand.Extensions.Reactive.Transform.System.Net.NetworkExtensions.HttpClient;
            set => Xpand.Extensions.Reactive.Transform.System.Net.NetworkExtensions.HttpClient = value;
        }

        public static IObservable<(HttpResponseMessage message, string content, object instance)> Object => Xpand.Extensions.Reactive.Transform.System.Net.NetworkExtensions.Object;
        public static IObservable<(HttpResponseMessage message, string content, T instance)> When<T>(
            this IObservable<(HttpResponseMessage message, string content, object instance)> source) 
            => source.Where(t => t.instance is T).Select(t => (t.message, t.content, ((T) t.instance)));

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.ConfigureRestOperationTypes()
                .RestOperationAction()
                .RestPropertyTypes()
                .ToUnit()
                .Merge(manager.WhenApplication(application => application.WhenNonPersistentObjectSpace()
                    .Merge(application.HandleObjectSpaceGetNonPersistentObject()
                    .Merge(application.FullTextSearch()
                    .Merge(application.ObjectStringLookup())
                    .Merge(application.EnableSortingGrouping())
                    .ToUnit()))));
        
        private static IObservable<Unit> ObjectStringLookup(this XafApplication application) 
            => application.WhenFrameViewChanged().WhenFrame(typeof(ObjectString),ViewType.ListView,Nesting.Nested)
                .SelectMany(frame => frame.View.AsListView().CollectionSource is NonPersistentPropertyCollectionSource source
                    ? source.MasterObjectType.RestListMembers().Where(t => t.attribute.PropertyName == source.MemberInfo.Name)
                        .SelectMany(_ => frame.GetController<NewObjectViewController>().NewObjectAction.WhenExecute()
                            .Merge(frame.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction.WhenExecuted())
                            .SelectMany(e => {
                                var dataSourceProperty = source.MemberInfo.FindAttribute<DataSourcePropertyAttribute>().DataSourceProperty;
                                var datasourceMember = source.MemberInfo.Owner.FindMember(dataSourceProperty);
                                var objectString = ((ObjectString) e.ShowViewParameters.CreatedView.CurrentObject);
                                var dynamicCollection = ((DynamicCollection) datasourceMember.GetValue(source.MasterObject));
                                return dynamicCollection.WhenObjects().IgnoreElements()
                                    .Merge(dynamicCollection.WhenLoaded())
                                    .Merge(dynamicCollection.Objects().Cast<ObjectString>().FirstOrDefault()
                                        .ReturnObservable().WhenNotDefault())
                                    .Do(_ => objectString.DataSource.AddRange(dynamicCollection.Objects().Cast<ObjectString>(), true));
                            })).ToUnit()
                    : Observable.Empty<Unit>())
                .ToUnit();

        static IObservable<Unit> HandleObjectSpaceGetNonPersistentObject(this XafApplication application)
            => application.WhenNonPersistentObjectSpaceCreated()
                .SelectMany(t => t.ObjectSpace.AsNonPersistentObjectSpace()
                    .WhenObjectGetting()
                    // .Where(tuple => tuple.e.TargetObject != tuple.e.SourceObject)
                    .SelectMany(tuple => {
                        tuple.e.TargetObject = tuple.e.SourceObject;
                        if (tuple.e.TargetObject is IObjectSpaceLink objectSpaceLink) {
                            objectSpaceLink.ObjectSpace = tuple.objectSpace;
                        }
                        return tuple.e.TargetObject.GetTypeInfo().Members.Where(info =>
                                typeof(IObjectSpaceLink).IsAssignableFrom(info.MemberType))
                            .ToObservable(Scheduler.Immediate)
                            .Select(info => info.GetValue(tuple.e.TargetObject)).WhenNotDefault()
                            .Cast<IObjectSpaceLink>()
                            .Do(link => link.ObjectSpace = tuple.objectSpace);
                    })
                    .ToUnit()
                );

        private static IObservable<Unit> EnableSortingGrouping(this XafApplication application)
            => application.WhenFrameViewControls()
                .WhenFrame(ViewType.ListView)
                .Where(frame => frame.View.AsListView().ObjectTypeInfo.FindAttributes<RestOperationAttribute>()
                    .Any(attribute => attribute.Operation == Operation.Get))
                .Select(frame => frame.View.AsListView().Editor).OfType<ColumnsListEditor>()
                .SelectMany(editor => editor.Columns)
                .Do(wrapper => {
                    wrapper.AllowSortingChange = true;
                    wrapper.AllowGroupingChange = true;
                })
                .ToUnit();

        private static IObservable<Unit> FullTextSearch(this XafApplication application) 
            => application.WhenFrameCreated().ToController<FilterController>()
                .SelectMany(controller => controller.WhenCreateCustomSearchCriteriaBuilder()
                    .Do(t => t.e.SearchCriteriaBuilder=new SearchCriteriaBuilder())).ToUnit();

        private static IObservable<Unit> WhenNonPersistentObjectSpace(this XafApplication application) 
            => application.WhenLoggedOn().SelectMany(_ => application.WhenNonPersistentObjectSpaceCreated()
                .SelectMany(t => t.ObjectSpace.WhenObjects(t1 => t1.WhenRestObjects(application))
                    .Select(o => o)
                    // .MergeIgnored(o => {
                    //     var nonPersistentBaseObject = ((NonPersistentBaseObject) o);
                    //     return nonPersistentBaseObject.WhenObjectSpaceChanged()
                    //         .ReactiveCollectionsInit(((IObjectSpaceLink) nonPersistentBaseObject).ObjectSpace);
                    // })
                    .RestPropertyDependentChange()
                    .RestPropertyBindingListsChange()
                    // .RestPropertyBindingListsDataSource(t.ObjectSpace,application) //slow
                    .ReactiveCollectionsFetch(application.GetCurrentUser<ICredentialBearer>())
                    .ToUnit().IgnoreElements()
                    .Merge(Observable.Defer(() => t.ObjectSpace.WhenCommitingObjects(o => t.ObjectSpace.Commit(o,application.GetCurrentUser<ICredentialBearer>()))))));

        private static IObservable<object> WhenRestObjects(
            this (NonPersistentObjectSpace objectSpace, ObjectsGettingEventArgs e) t1, XafApplication application) 
            => t1.objectSpace.Get(t1.e.ObjectType,application.GetCurrentUser<ICredentialBearer>())
                .RestPropertyDependent(t1.objectSpace,t1.e.ObjectType,application.GetCurrentUser<ICredentialBearer>())
                .RestPropertyBindingListsInit(t1.objectSpace)
                .ReactiveCollectionsInit(t1.objectSpace)
            ;

        internal static IObservable<TSource> TraceRestModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, RestModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
    }

    public interface ICredentialBearer {
        string BaseAddress { get; set; }
        string Key { get; set; }
        string Secret { get; set; }
    }
}
