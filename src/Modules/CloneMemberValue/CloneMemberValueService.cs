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
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.CloneMemberValue{
    public static class CloneMemberValueService{

        public static IObservable<Unit> Connect(this ApplicationModulesManager modulesManager ,XafApplication application){
            if (application != null){
                return application.WhenCloneMemberValues()
                    .ToUnit();
            }
            return Observable.Empty<Unit>();
        }
        internal static IObservable<TSource> TraceCloneMemberValueModule<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, CloneMemberValueModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

        public static IEnumerable<IModelCommonMemberViewItemCloneValue> CloneValueMemberViewItems(this ObjectView objectView){
            return (objectView is ListView view
                    ? view.Model.Columns.Cast<IModelCommonMemberViewItemCloneValue>()
                    : ((DetailView) objectView).Model.Items.OfType<IModelCommonMemberViewItemCloneValue>())
                .Where(state => state.CloneValue)
                .Select(value => value);
        }

        public static IObservable<(ObjectView objectView, IMemberInfo MemberInfo, object previousObject, object currentObject)> CloneMembers(
            this IObservable<(ObjectView objectView,object previous, object current)> source){

            return source.SelectMany(_ => _.objectView
                .CloneValueMemberViewItems()
                .Select(value =>(_.objectView, ((IModelMemberViewItem) value).ModelMember.MemberInfo, _.previous, _.current).CloneMemberValue()));
        }

        public static IObservable<(object previous, object current)> NewObjectPairs(this ListEditor listEditor){
            return listEditor.WhenNewObjectAdding()
                .Select(_ => _.e.AddedObject).Cast<object>()
                .CombineWithPrevious().Where(_ => _.previous != null);
        }

        public static IObservable<(DetailView previous, DetailView current)> WhenCloneMemberValueDetailViewPairs(this XafApplication application){
            return application
                .WhenDetailViewCreated()
                .Select(_ => _.e.View).Where(view => view.CloneValueMemberViewItems().Any())
                .CombineWithPrevious().Where(_ => _.previous != null&&_.current.ObjectSpace.IsNewObject(_.current.CurrentObject))
                .Publish().RefCount();
        }

        public static IObservable<ListView> WhenCloneMemberValueListViewCreated(this XafApplication application){
            return application
                .WhenListViewCreated().Where(_ => _.e.ListView.CloneValueMemberViewItems().Any())
                .Select(_ => _.e.ListView).Where(view => view.Model.AllowEdit);
        }

        public static IObservable<(ObjectView objectView, IMemberInfo MemberInfo, object previousObject, object currentObject)>
            WhenCloneMemberValues(this XafApplication application){
            return application.WhenCloneMemberValueDetailViewPairs()
                .Select(_ => (((ObjectView) _.current),_.previous.CurrentObject,_.current.CurrentObject))
                .Merge(application.WhenCloneMemberValueListViewCreated()
                    .ControlsCreated()
                    .SelectMany(_ => (_.view.Editor
                        .NewObjectPairs()
                        .Select(tuple => (((ObjectView) _.view),tuple.previous,tuple.current)))))
                .CloneMembers()
                .Publish().RefCount();
        }

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