using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using ObjectSpaceExtensions = Xpand.Extensions.XAF.ObjectSpaceExtensions.ObjectSpaceExtensions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire {
    static class ExecuteJobActionExtensions {
        internal static IObservable<Unit> ExecuteAction(this XafApplication application,string jobId) 
            => Observable.Using(() => application.CreateNonSecuredObjectSpace(typeof(ExecuteActionJob)), objectSpace 
                => objectSpace.GetObjectsQuery<ExecuteActionJob>().Where(actionJob => actionJob.Id == jobId).ToArray().ToNowObservable()
                .ConcatIgnored(job => {
                    if (job.AuthenticateUserCriteria == null) return job.Observe();
                    var user = objectSpace.FindObject(SecuritySystem.UserType, CriteriaOperator.Parse(job.AuthenticateUserCriteria));
                    return user != null ? application.LogonUser(objectSpace.GetKeyValue(user)).TakeFirst().To(job)
                        : Observable.Throw<Job>(new Exception($"{nameof(user)} not found"));
                })
                .SelectMany(job => application.CreateView(job, application.Model.Views[job.View.Name]).ToUnit()
                    .Merge(application.ExecuteAction(job)))
                .ToUnit());

        private static IObservable<View> CreateView(this XafApplication application,ExecuteActionJob job, IModelView modelView) 
            => application.WhenViewCreating()
                .SelectItemResilient(t => {
                    var objectType = modelView.AsObjectView.ModelClass.TypeInfo.Type;
                    t.e.View = application.CreateView( job, modelView, application.CreateObjectSpace(objectType,true), objectType);
                    return t.e.View;
                }).TakeFirst().IgnoreElements();

        private static ObjectView CreateView(this XafApplication application, ExecuteActionJob job, IModelView modelView, IObjectSpace space, Type objectType) 
            => modelView is IModelListView modelListView
                ? new ListView(modelListView, application.CreateCollectionSource(space, objectType, modelView.Id), application, job.SelectedObjectsCriteria)
                : new DetailView((IModelDetailView)modelView, space, null, application, true);

        class ListView(
            IModelListView modelListView,
            CollectionSourceBase collectionSource,
            XafApplication application,
            string selectedObjectsCriteria)
            : DevExpress.ExpressApp.ListView(modelListView, collectionSource, application, true) {
            public override SelectionType SelectionType => SelectionType.MultipleSelection;
            public override IList SelectedObjects 
                => CollectionSource.Objects()
                    .Where(o => ObjectSpaceExtensions.IsObjectFitForCriteria(ObjectSpace, CriteriaOperator.Parse(selectedObjectsCriteria), o))
                    .ToArray();
        }
        
        private static IObservable<Unit> ListViewExecute(this ActionBase action) 
            => action.WhenExecuteFinished().TakeFirst().ToUnit()
                .Merge(Unit.Default.Observe().DoItemResilient(_ => action.DoTheExecute()).IgnoreElements());

        private static IObservable<Unit> DetailViewExecute(this ActionBase action, ExecuteActionJob job, CompositeView newView) {
            var objects = newView.ObjectSpace
                .GetObjects(newView.ObjectTypeInfo.Type, CriteriaOperator.Parse(job.SelectedObjectsCriteria))
                .Cast<object>().ToArray();
            return action.WhenExecuteFinished().Select(a => a)
                .Take(objects.Length).ToUnit()
                .Merge(objects.ToNowObservable().DoItemResilient(o => {
                    newView.CurrentObject = o;
                    action.DoTheExecute();
                }).ToUnit().IgnoreElements());
        }
        
        private static IObservable<Unit> ExecuteAction(this XafApplication application, ExecuteActionJob job) 
            => Unit.Default.Observe()
                .SelectManyItemResilient(_ => {
                    var modelView = application.Model.Views[job.View.Name];
                    var newView = application.NewView(modelView.ViewType(),
                        modelView.AsObjectView.ModelClass.TypeInfo.Type);
                    var window = application.CreateViewWindow();
                    window.SetView(newView);
                    var action = window.Action(job.Action.Name);
                    if (action is SingleChoiceAction singleChoiceAction) {
                        singleChoiceAction.SelectedItem = singleChoiceAction.Items.First();
                    }
                    return newView is ListView ? action.ListViewExecute() : action.DetailViewExecute(job, newView);
                });
        
    }
}