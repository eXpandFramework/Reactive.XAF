using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.CloneMemberValue{
    public static class CloneMemberValueService{
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager modulesManager ) 
            => modulesManager.WhenApplication(application => application.WhenCloneMemberValues().ToUnit());

        internal static IObservable<TSource> TraceCloneMemberValueModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, CloneMemberValueModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
        
        public static IEnumerable<IModelCommonMemberViewItemCloneValue> CloneValueMemberViewItems(this ObjectView objectView) 
            => (objectView is ListView view
                ? view.Model.Columns.Cast<IModelCommonMemberViewItemCloneValue>()
                : ((DetailView) objectView).Model.Items.OfType<IModelCommonMemberViewItemCloneValue>())
            .Where(state => state.CloneValue)
            .Select(value => value);

        public static IObservable<(ObjectView objectView, IMemberInfo MemberInfo, object previousObject, object currentObject)> CloneMembers(
            this IObservable<(ObjectView objectView,object previous, object current)> source) 
            => source.SelectMany(t => t.objectView
                .CloneValueMemberViewItems().ToNowObservable()
                .SelectItemResilient(value =>(t.objectView, ((IModelMemberViewItem) value).ModelMember.MemberInfo, t.previous, t.current).CloneMemberValue()));

        public static IObservable<(object previous, object current)> NewObjectPairs(this ListEditor listEditor) 
            => listEditor.WhenNewObjectAdding()
                .Select(e => e.AddedObject).Cast<object>()
                .CombineWithPrevious().Where(t => t.previous != null);

        public static IObservable<(DetailView previous, DetailView current)> WhenCloneMemberValueDetailViewPairs(this XafApplication application) 
            => application
                .WhenDetailViewCreated()
                .Select(t => t.e.View).Where(view => view.CloneValueMemberViewItems().Any())
                .CombineWithPrevious().Where(t => t.previous != null&&t.current.ObjectSpace.IsNewObject(t.current.CurrentObject))
                .Publish().RefCount();

        public static IObservable<ListView> WhenCloneMemberValueListViewCreated(this XafApplication application) 
            => application
                .WhenListViewCreated().Where(listView => listView.CloneValueMemberViewItems().Any())
                .Select(listView => listView).Where(view => view.Model.AllowEdit);

        public static IObservable<(ObjectView objectView, IMemberInfo MemberInfo, object previousObject, object currentObject)> WhenCloneMemberValues(this XafApplication application) 
            => application.WhenCloneMemberValueDetailViewPairs()
                .Select(t => (((ObjectView) t.current),t.previous.CurrentObject,t.current.CurrentObject))
                .Merge(application.WhenCloneMemberValueListViewCreated()
                    .WhenControlsCreated()
                    .SelectMany(listView => (listView.Editor
                        .NewObjectPairs()
                        .Select(tuple => (((ObjectView) listView),tuple.previous,tuple.current)))))
                .CloneMembers()
                .TraceCloneMemberValueModule(t => $"{t.objectView.Id}, {t.MemberInfo.Name}, p={t.previousObject}, c={t.currentObject}")
            ;

        private static (ObjectView objectView,IMemberInfo MemberInfo, object previousObject, object currentObject)
            CloneMemberValue(this (ObjectView objectView,IMemberInfo MemberInfo, object previousObject, object currentObject) _){

            var value = _.MemberInfo.GetValue(_.previousObject);
            if (_.MemberInfo.MemberTypeInfo.IsPersistent){
                value = _.objectView.ObjectSpace.GetObject(value);
            }
            _.MemberInfo.SetValue(_.currentObject, value);
            return (_.objectView,_.MemberInfo, _.previousObject, _.currentObject);
        }
    }
}