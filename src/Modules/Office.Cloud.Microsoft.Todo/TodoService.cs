using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using Microsoft.Graph;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo{
    public static class TodoService{
        private static readonly ISubject<(Frame frame, GraphServiceClient client)> ClientSubject=new Subject<(Frame frame, GraphServiceClient client)>();

        public static IObservable<(Frame frame, GraphServiceClient client)> Client => ClientSubject.AsObservable();
        
        internal const string DefaultTodoListId = "Tasks";
        private static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        private static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;

        static readonly Subject<(OutlookTask serviceObject, MapAction mapAction)> UpdatedSubject=new Subject<(OutlookTask serviceObject, MapAction mapAction)>();
        public static IObservable<(OutlookTask cloud, MapAction mapAction)> Updated{ get; }=UpdatedSubject.AsObservable();
        static readonly Subject<GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)>> CustomizeSynchronizationSubject =
            new Subject<GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)>>();

        public static IObservable<GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)>> CustomizeSynchronization 
            => CustomizeSynchronizationSubject.AsObservable();

        public static IObservable<(OutlookTask cloud, MapAction mapAction)> When(this IObservable<(OutlookTask outlookTask, MapAction mapAction)> source, MapAction mapAction)
            => source.Where(_ => _.mapAction == mapAction);
        public static IObservable<GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)>> When(
            this IObservable<GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)>> source, MapAction mapAction)
            => source.Where(_ => _.Instance.mapAction == mapAction);
        
        private static IObservable<(OutlookTask serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<IOutlookTaskFolderRequestBuilder> source,IModelTodoItem modelTodoItem, 
            IObjectSpace objectSpace, Func<IObjectSpace> objectSpaceFactory) 
            => source.SelectMany(builder => objectSpaceFactory.SynchronizeCloud<OutlookTask, ITask>(modelTodoItem.SynchronizationType,objectSpace,
                cloudId => ((IOutlookTaskRequest)RequestCustomization.Default(builder.Me().Outlook.Tasks[cloudId].Request())).DeleteAsync().ToObservable(),
                task => ((IOutlookTaskFolderTasksCollectionRequest)RequestCustomization.Default(builder.Tasks.Request())).AddAsync(task).ToObservable(),
                cloudId => new OutlookTask().ReturnObservable(),
                t => ((IOutlookTaskRequest)RequestCustomization.Default(builder.Me().Outlook.Tasks[t.cloudId].Request())).UpdateAsync(t.cloudEntity).ToObservable(),
                e => e.Handled=MapAction.Delete.CustomSynchronization(e.Instance.cloudOfficeObject.ObjectSpace, e.Instance.localEntinty, null).Handled,
                t => MapAction.Insert.CustomSynchronization(null, t.source, t.target),
                t => MapAction.Update.CustomSynchronization(null, t.source, t.target)));

        private static GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)> CustomSynchronization(
            this MapAction mapAction,IObjectSpace objectSpace, ITask local, OutlookTask cloud){
            var e = new GenericEventArgs<(IObjectSpace objectSpace, ITask local, OutlookTask cloud, MapAction mapAction)>((objectSpace, local, cloud, mapAction));
            CustomizeSynchronizationSubject.OnNext(e);
            if (!e.Handled){
                if (e.Instance.mapAction!=MapAction.Delete){
                    cloud.Subject = local.Subject;
                    cloud.Body = new ItemBody { Content = local.Description };
                    cloud.Status = local.Status == TaskStatus.Completed ? global::Microsoft.Graph.TaskStatus.Completed : global::Microsoft.Graph.TaskStatus.NotStarted;
                    if (local.DateCompleted != DateTime.MinValue){
                        cloud.CompletedDateTime = new DateTimeTimeZone(){
                            DateTime = local.DateCompleted.ToString(CultureInfo.InvariantCulture),
                            TimeZone = TimeZoneInfo.Local.Id
                        };
                    }
                    if (local.StartDate != DateTime.MinValue){
                        cloud.StartDateTime = new DateTimeTimeZone(){
                            DateTime = local.StartDate.ToString(CultureInfo.InvariantCulture),
                            TimeZone = TimeZoneInfo.Local.Id
                        };
                    }
                    if (local.DueDate != DateTime.MinValue){
                        cloud.DueDateTime = new DateTimeTimeZone(){
                            DateTime = local.DueDate.ToString(CultureInfo.InvariantCulture),
                            TimeZone = TimeZoneInfo.Local.Id
                        };
                    }
                }
            }

            return e;
        }

        public static IObservable<OutlookTaskFolder> GetFolder(this IOutlookUserTaskFoldersCollectionRequestBuilder builder, string name, bool createNew = false){
            var request = builder.Request();
            var addNew = createNew.ReturnObservable().WhenNotDefault().SelectMany(b => request.AddAsync(new OutlookTaskFolder() { Name = name }));
            return request.Filter($"{nameof(OutlookTaskFolder.Name)} eq '{name}'").GetAsync().ToObservable(ImmediateScheduler.Instance)
                .SelectMany(page => page).SwitchIfEmpty(addNew).FirstOrDefaultAsync().Publish().RefCount();
        }

        internal static IObservable<TSource> TraceMicrosoftTodoModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, MicrosoftTodoModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) =>
            manager.WhenApplication(application => application.WhenViewOnFrame()
                .When(frame => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo().Items.Select(item => item.ObjectView))
                .Authorize()
                .SynchronizeCloud()
                .ToUnit());

        private static IObservable<(Frame frame, GraphServiceClient client, OutlookTaskFolder folder)> EnsureTaskFolder(this IObservable<(Frame frame, GraphServiceClient client)> source) =>
            source.Select(_ => Observable.Start(() => _.client.Me.Outlook.TaskFolders
                        .GetFolder(_.frame.View.AsObjectView().Application().Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft()
                            .Todo().DefaultTodoListName, true)).Merge().Wait().ReturnObservable()
                    .Select(folder => (_.frame,_.client,folder))
                ).Merge()
                .TraceMicrosoftTodoModule(folder => folder.folder.Name);

        private static IObservable<(OutlookTask serviceObject, MapAction mapAction)> SynchronizeCloud(this IObservable<(Frame frame, GraphServiceClient client, OutlookTaskFolder folder,IModelTodoItem modelTodoItem)> source) =>
            source.Select(client => client.client.Me.Outlook.TaskFolders[client.folder.Id].ReturnObservable().SynchronizeCloud(client.modelTodoItem,
                        client.frame.View.ObjectSpace, client.frame.View.AsObjectView().Application().CreateObjectSpace)
                    .TakeUntil(client.frame.View.WhenClosing())
                ).Switch()
                .Do(UpdatedSubject.OnNext)
                .TraceMicrosoftTodoModule(_ => $"{_.mapAction} {_.serviceObject.Subject}, {_.serviceObject.Status}, {_.serviceObject.Id}");
        
        static IObservable<(Frame frame, GraphServiceClient client, OutlookTaskFolder folder, IModelTodoItem modelTodoItem)> Authorize(this  IObservable<Frame> source) => source
            .AuthorizeMS().EnsureTaskFolder()
            .Publish().RefCount()
            .Do(tuple => ClientSubject.OnNext((tuple.frame,tuple.client)))
            .Select(t => (t.frame,t.client,t.folder,t.frame.Application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo().Items[t.frame.View.Id]))
            .TraceMicrosoftTodoModule(_ => _.frame.View.Id);
    }
}