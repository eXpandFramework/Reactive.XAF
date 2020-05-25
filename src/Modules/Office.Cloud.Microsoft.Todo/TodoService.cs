using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Microsoft.Graph;
using Xpand.Extensions.EventArg;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.Microsoft;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo{
    public static class TodoService{
        private static readonly ISubject<(OutlookTask, ITask)> CustomizeCloudSynchronizationSubject;
        static TodoService() => CustomizeCloudSynchronizationSubject = Subject.Synchronize(new Subject<(OutlookTask, ITask)>());
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
        public static IObservable<(Frame frame, GraphServiceClient client)> Client{ get; private set; }
        public static IObservable<OutlookTask> Updated{ get; private set; }

        public static IObservable<OutlookTask> SynchronizeCloud(this IObservable<IOutlookTaskFolderRequestBuilder> source, IObjectSpace objectSpace,
            Func<IObjectSpace> objectSpaceFactory, Action<GenericEventArgs<CloudOfficeObject>> delete = null,
            Action<(OutlookTask target, ITask source)> insert = null, Action<(OutlookTask target, ITask source)> update = null) =>
            source.SelectMany(builder => objectSpaceFactory.Synchronize(objectSpace,
                id => ((IOutlookTaskRequest)RequestCustomization.Default(builder.Me().Outlook.Tasks[id].Request())).DeleteAsync().ToObservable(),
                task => ((IOutlookTaskFolderTasksCollectionRequest)RequestCustomization.Default(builder.Tasks.Request())).AddAsync(task).ToObservable(),
                id => new OutlookTask().ReturnObservable(),
                _ => ((IOutlookTaskRequest)RequestCustomization.Default(builder.Me().Outlook.Tasks[_.cloudId].Request())).UpdateAsync(_.cloudEntity).ToObservable(),
                (target, sourceEntity) => CustomizeCloudSynchronizationSubject.SynchronizeCloud(target, sourceEntity), delete, insert, update));

        [PublicAPI]
        public static IObservable<(OutlookTask target, ITask source)> CustomizeCloudSynchronization { get; } = CustomizeCloudSynchronizationSubject;

        private static OutlookTask SynchronizeCloud(this ISubject<(OutlookTask, ITask)> subject, OutlookTask target, ITask source){
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
            subject.OnNext((target, source));
            return target;
        }

        public static IObservable<OutlookTaskFolder> GetFolder(this IOutlookUserTaskFoldersCollectionRequestBuilder builder, string name, bool createNew = false){
            var request = builder.Request();
            var addNew = createNew.ReturnObservable().WhenNotDefault().SelectMany(b =>
                request.AddAsync(new OutlookTaskFolder() { Name = name }).ToObservable());
            return request.Filter($"{nameof(OutlookTaskFolder.Name)} eq '{name}'").GetAsync().ToObservable()
                .SelectMany(page => page).SwitchIfEmpty(addNew).FirstOrDefaultAsync().Select(folder => folder);
        }

        internal static IObservable<TSource> TraceMicrosoftTodoModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, MicrosoftTodoModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this XafApplication application){
            Client = application.AuthorizationRequired()
                .Authorize()
                .Retry(application)
                .Publish().RefCount();
            Updated = Client.SynchronizeCloud().Publish().RefCount();
            return Updated
                .TraceMicrosoftTodoModule(task => $"{task.Subject}, {task.Status}, {task.Id}") 
                .ToUnit();
        }
        
        private static IObservable<OutlookTask> SynchronizeCloud(this IObservable<(Frame frame,GraphServiceClient client)> source) =>
            source.SelectMany(client => {
                var modelTodo = client.frame.Application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Microsoft().Todo();
                return client.client.Me.Outlook.TaskFolders.GetFolder(modelTodo.DefaultTodoListName)
                    .Select(folder => client.client.Me.Outlook.TaskFolders[folder.Id]).SynchronizeCloud(
                        client.frame.View.ObjectSpace, client.frame.Application.CreateObjectSpace,
                        CustomizeDeleteSubject.OnNext, CustomizeInsertSubject.OnNext, CustomizeUpdateSubject.OnNext);
            }).TraceMicrosoftTodoModule(task => $"{task.Subject}, {task.Status}, {task.Id}");

        private static IObservable<Frame> AuthorizationRequired(this XafApplication application) => application
            .WhenViewOnFrame()
            .Where(frame => {
                var moduleOffice = application.Model.ToReactiveModule<IModelReactiveModuleOffice>();
                return moduleOffice.Microsoft().Todo().ObjectViews().Select(dependency => dependency.Id())
                    .Contains(frame.View.Model.Id);
            })
            .TraceMicrosoftTodoModule(frame => frame.View.Id);

        static IObservable<(Frame frame, GraphServiceClient client)> Authorize(this  IObservable<Frame> whenViewOnFrame) => whenViewOnFrame
            .SelectMany(frame => frame.Application.AuthorizeMS().Select(client => (frame, client))).TraceMicrosoftTodoModule();
    }
}