using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
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

        private static readonly ISubject<(MapAction mapAction,OutlookTask outlookTask, ITask task)> CustomizeCloudSynchronizationSubject;
        static TodoService() => CustomizeCloudSynchronizationSubject = Subject.Synchronize(new Subject<(MapAction mapAction,OutlookTask, ITask)>());
        internal const string DefaultTodoListId = "Tasks";
        private static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        private static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;
        static readonly ISubject<GenericEventArgs<CloudOfficeObject>> CustomizeDeleteSubject=Subject.Synchronize(new Subject<GenericEventArgs<CloudOfficeObject>>());
        static readonly ISubject<(OutlookTask outlookTask,ITask task)> CustomizeInsertSubject=Subject.Synchronize(new Subject<(OutlookTask outlookTask, ITask task)>());
        static readonly ISubject<(OutlookTask outlookTask,ITask task)> CustomizeUpdateSubject=Subject.Synchronize(new Subject<(OutlookTask outlookTask, ITask task)>());

        [PublicAPI]
        public static IObservable<GenericEventArgs<CloudOfficeObject>> CustomizeDelete => CustomizeDeleteSubject.AsObservable();
        [PublicAPI]
        public static IObservable<(OutlookTask outlookTask, ITask task)> CustomizeUpdate => CustomizeUpdateSubject.AsObservable();
        [PublicAPI]
        public static IObservable<(OutlookTask outlookTask, ITask task)> CustomizeInsert => CustomizeInsertSubject.AsObservable();
        
        static readonly Subject<(OutlookTask serviceObject, MapAction mapAction)> UpdatedSubject=new Subject<(OutlookTask serviceObject, MapAction mapAction)>();

        public static IObservable<(OutlookTask serviceObject, MapAction mapAction)> Updated{ get; }=UpdatedSubject.AsObservable();

        private static IObservable<(OutlookTask serviceObject, MapAction mapAction)> Synchronize(this IObservable<IOutlookTaskFolderRequestBuilder> source, IObjectSpace objectSpace,
            Func<IObjectSpace> objectSpaceFactory, Action<GenericEventArgs<CloudOfficeObject>> delete = null,
            Action<(OutlookTask target, ITask source)> insert = null, Action<(OutlookTask target, ITask source)> update = null) =>
            source.SelectMany(builder => objectSpaceFactory.Synchronize(objectSpace,
                id => ((IOutlookTaskRequest)RequestCustomization.Default(builder.Me().Outlook.Tasks[id].Request())).DeleteAsync().ToObservable(),
                task => ((IOutlookTaskFolderTasksCollectionRequest)RequestCustomization.Default(builder.Tasks.Request())).AddAsync(task).ToObservable(),
                id => new OutlookTask().ReturnObservable(),
                _ => ((IOutlookTaskRequest)RequestCustomization.Default(builder.Me().Outlook.Tasks[_.cloudId].Request())).UpdateAsync(_.cloudEntity).ToObservable(),
                (mapAction,cloud, local) =>CustomizeCloudSynchronizationSubject.Synchronize(cloud, local,mapAction) , delete, insert, update));

        [PublicAPI]
        public static ISubject<(MapAction mapAction, OutlookTask outlookTask, ITask task)> CustomizeCloudSynchronization { get; } = CustomizeCloudSynchronizationSubject;

        private static OutlookTask Synchronize(this ISubject<(MapAction,OutlookTask, ITask)> subject, OutlookTask target, ITask source,MapAction mapAction){
            target.Subject = source.Subject;
            target.Body = new ItemBody { Content = source.Description };
            target.Status = source.Status == TaskStatus.Completed
                ? global::Microsoft.Graph.TaskStatus.Completed
                : global::Microsoft.Graph.TaskStatus.NotStarted;
            if (source.DateCompleted != DateTime.MinValue){
                target.CompletedDateTime = new DateTimeTimeZone(){
                    DateTime = source.DateCompleted.ToString(CultureInfo.InvariantCulture),
                    TimeZone = TimeZoneInfo.Local.Id
                };
            }

            if (source.StartDate != DateTime.MinValue){
                target.StartDateTime = new DateTimeTimeZone(){
                    DateTime = source.StartDate.ToString(CultureInfo.InvariantCulture),
                    TimeZone = TimeZoneInfo.Local.Id
                };
            }

            if (source.DueDate != DateTime.MinValue){
                target.DueDateTime = new DateTimeTimeZone(){
                    DateTime = source.DueDate.ToString(CultureInfo.InvariantCulture),
                    TimeZone = TimeZoneInfo.Local.Id
                };
            }
            subject.OnNext((mapAction,target, source));
            return target;
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
                .When(frame => application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().Todo().ObjectViews())
                .Authorize()
                .Synchronize()
                .ToUnit());

        private static IObservable<(Frame frame, GraphServiceClient client, OutlookTaskFolder folder)> EnsureTaskFolder(this IObservable<(Frame frame, GraphServiceClient client)> source) =>
            source.Select(_ => Observable.Start(() => _.client.Me.Outlook.TaskFolders
                        .GetFolder(_.frame.View.AsObjectView().Application().Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft()
                            .Todo().DefaultTodoListName, true)).Merge().Wait().ReturnObservable()
                    .Select(folder => (_.frame,_.client,folder))
                ).Merge()
                .TraceMicrosoftTodoModule(folder => folder.folder.Name);

        private static IObservable<(OutlookTask serviceObject, MapAction mapAction)> Synchronize(this IObservable<(Frame frame, GraphServiceClient client, OutlookTaskFolder folder)> source) =>
            source.Select(client => client.client.Me.Outlook.TaskFolders[client.folder.Id].ReturnObservable().Synchronize(
                        client.frame.View.ObjectSpace, client.frame.View.AsObjectView().Application().CreateObjectSpace,
                        CustomizeDeleteSubject.OnNext, CustomizeInsertSubject.OnNext,
                        CustomizeUpdateSubject.OnNext)
                    .TakeUntil(client.frame.View.WhenClosing())
                ).Switch()
                .Do(UpdatedSubject)
                .TraceMicrosoftTodoModule(_ => $"{_.mapAction} {_.serviceObject.Subject}, {_.serviceObject.Status}, {_.serviceObject.Id}");
        
        static IObservable<(Frame frame, GraphServiceClient client, OutlookTaskFolder folder)> Authorize(this  IObservable<Frame> whenViewOnFrame) => whenViewOnFrame
            .AuthorizeMS().EnsureTaskFolder()
            .Do(tuple => ClientSubject.OnNext((tuple.frame,tuple.client)))
            .TraceMicrosoftTodoModule(_ => _.frame.View.Id);
    }
}