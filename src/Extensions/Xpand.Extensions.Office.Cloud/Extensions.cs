using System;

using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.Extensions.Office.Cloud{

    public static class Extensions{
        // private const string MSGraphMeUri = "https://graph.microsoft.com/beta/me/";
        public static void SaveToken(this ITokenStore store, Func<IObjectSpace> objectSpaceFactory){
            using (var space = objectSpaceFactory()){
                var storage = (ITokenStore)(space.GetObject(store) ?? space.CreateObject(store.GetType()));
                storage.Token = store.Token;
                storage.EntityName = store.EntityName;
                space.CommitChanges();
            }
        }

        static Extensions() => System.AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        private static System.Reflection.Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args){
            if (args.Name.Contains("Newton")){
                return System.Reflection.Assembly.LoadFile($@"{System.AppDomain.CurrentDomain.BaseDirectory}bin\Newtonsoft.Json.dll");
            }
            return null;
        }

        public static IObservable<TCloudEntity> MapEntity<TCloudEntity, TLocalEntity>(this Func<IObjectSpace> objectSpaceFactory, TLocalEntity localEntity,
            Func<TLocalEntity, IObservable<TCloudEntity>> insert, Func<(string cloudId, TLocalEntity task), IObservable<TCloudEntity>> update){
            var objectSpace = objectSpaceFactory();
            var localId = objectSpace.GetKeyValue(localEntity).ToString();
            var cloudId = objectSpace.GetCloudId(localId, localEntity.GetType());
            return cloudId == null ? insert(localEntity) : update((cloudId, localEntity));
        }

        // public static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        // [PublicAPI]
        // public static IUserRequestBuilder Me(this IBaseRequest builder) => builder.Client.Me();
        // [PublicAPI]
        // public static IObservable<T> Add<T>(this IBaseClient client, T entity, string path) => client.Update(entity, path, HttpMethod.Post).Select(arg => arg);
        // [PublicAPI]
        // public static IObservable<T> Get<T>(this IBaseClient client, string uri)
        // {
        //     var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"{MSGraphMeUri}{uri}");
        //     return client.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage).ToObservable()
        //         .SelectMany(unit => client.HttpProvider
        //             .SendAsync(httpRequestMessage).ToObservable()
        //             .Do(message => message.EnsureSuccessStatusCode())
        //             .SelectMany(message => message.Content.ReadAsStringAsync())
        //             .Select(JsonConvert.DeserializeObject<T>));
        // }
        // [PublicAPI]
        // public static IObservable<Unit> Delete(this IBaseClient client, string uri)
        // {
        //     var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{MSGraphMeUri}{uri}");
        //     return client.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage).ToObservable()
        //         .SelectMany(_ => client.HttpProvider
        //             .SendAsync(httpRequestMessage).ToObservable()
        //             .Do(message => message.EnsureSuccessStatusCode())
        //             .ToUnit());
        // }
        // [PublicAPI]
        // public static IObservable<T> Update<T>(this IBaseClient client, T entity, string uri, HttpMethod httpMethod)
        // {
        //     HttpContent StringContent(HttpRequestMessage request)
        //     {
        //         string json = JsonConvert.SerializeObject(entity);
        //         var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        //         request.Content = stringContent;
        //         return stringContent;
        //     }
        //
        //     var httpRequestMessage = new HttpRequestMessage(httpMethod, $"{MSGraphMeUri}{uri}");
        //     return client.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage).ToObservable()
        //         .SelectMany(_ => Observable.Using(() => httpRequestMessage,
        //             request => Observable.Using(() => StringContent(request), stringContent => Observable.FromAsync(() =>
        //                     client.HttpProvider.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None))
        //                 .Do(message => message.EnsureSuccessStatusCode())
        //                 .SelectMany(message => message.Content.ReadAsStringAsync())
        //                 .Select(JsonConvert.DeserializeObject<T>))));
        // }
        // [PublicAPI]
        // public static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;
        //
        // public static IObservable<TResponse> ToObservable<TResponse>(this ClientServiceRequest request) => ((IClientServiceRequest<TResponse>)request).ToObservable();
        //
        // public static IObservable<TResponse> ToObservable<TResponse>(this IClientServiceRequest<TResponse> request) => request.ExecuteAsync().ToObservable();
        //
        [PublicAPI]
        public static IObservable<T> DeleteObjectSpaceLink<T>(this IObservable<T> source) where T : IObjectSpaceLink => source.Select(link => {
            link.ObjectSpace.Delete(link);
            link.ObjectSpace.CommitChanges();
            return link;
        });

        public static IObservable<TServiceObject> MapEntities<TBO, TServiceObject>(this IObjectSpace objectSpace,
            Func<CloudOfficeObject, IObservable<TServiceObject>> delete, Func<TBO, IObservable<TServiceObject>> map){
            var deleteObjects = objectSpace.WhenDeletedObjects<TBO>(true)
                .SelectMany(_ => _.objects.SelectMany(o => {
	                var deletedId = _.objectSpace.GetKeyValue(o).ToString();
	                return _.objectSpace.QueryCloudOfficeObject(typeof(TServiceObject), o).Where(officeObject => officeObject.LocalId == deletedId);
                }))
                .DeleteObjectSpaceLink()
                .SelectMany(cloudOfficeObject => delete(cloudOfficeObject).Select(s => cloudOfficeObject))
                .To((TServiceObject)typeof(TServiceObject).CreateInstance());
            var mapObjects = objectSpace.WhenModifiedObjects<TBO>(true, ObjectModification.NewOrUpdated)
                .SelectMany(_ => _.objects).Cast<TBO>().SelectMany(map);
            return mapObjects.Merge(deleteObjects);
        }
        public static IObservable<TServiceObject> MapEntities<TBO, TServiceObject>(this IObjectSpace objectSpace,IObservable<TBO> deletedObjects,
            IObservable<TBO> newOrUpdatedObjects, Func<CloudOfficeObject, IObservable<TServiceObject>> delete, Func<TBO, IObservable<TServiceObject>> map){
            return newOrUpdatedObjects.SelectMany(map)
                .Merge(deletedObjects
                    .SelectMany(_ => {
                        var deletedId = objectSpace.GetKeyValue(_).ToString();
                        return objectSpace.QueryCloudOfficeObject(typeof(TServiceObject), _).Where(o => o.LocalId == deletedId).ToObservable();
                    })
                    .DeleteObjectSpaceLink()
                    .SelectMany(cloudOfficeObject => delete(cloudOfficeObject).Select(s => cloudOfficeObject))
                    .To<TServiceObject>());
        }

        public static IObservable<T> NewCloudObject<T>(this IObservable<T> source, Func<IObjectSpace> objectSpaceFactory, string localId) => source
            .SelectMany(@event => Observable.Using(objectSpaceFactory, 
                space => space.NewCloudObject(localId, (string)@event.GetPropertyValue("Id"), @event.GetType().ToCloudObjectType())
                    .Select(unit => @event)));
        [PublicAPI]
        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, string localId, string cloudId, System.Type cloudObjectType) => space
            .NewCloudObject(localId, cloudId, cloudObjectType.ToCloudObjectType());

        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, object localEntity, object cloudEntity){
            var localId = space.GetKeyValue(localEntity).ToString();
            var cloudId = cloudEntity.GetPropertyValue("Id").ToString();
            return space.NewCloudObject(localId, cloudId, cloudEntity.GetType().ToCloudObjectType());
        }
        [PublicAPI]
        public static IObservable<CloudOfficeObject> NewCloudObject(this IObjectSpace space, string localId, string cloudId, CloudObjectType cloudObjectType){
            var cloudObject = space.CreateObject<CloudOfficeObject>();
            cloudObject.LocalId = localId;
            cloudObject.CloudId = cloudId;
            cloudObject.CloudObjectType = cloudObjectType;
            space.CommitChanges();
            return cloudObject.ReturnObservable();
        }
        [PublicAPI]
        public static string GetCloudId(this IObjectSpace objectSpace, string localId, System.Type cloudEntityType) => objectSpace.QueryCloudOfficeObject(localId, cloudEntityType).FirstOrDefault()?.CloudId;

        // private static readonly IScheduler RegularEventsScheduler = Scheduler.Immediate;
        public static object KeyValue<T>(this T t) where T : IObjectSpaceLink => t.ObjectSpace.GetKeyValue(t);

        // public static IObservable<T> SwitchIfDefault<T>(this IObservable<T> @this, IObservable<T> switchTo) where T : class{
        //     if (@this == null) throw new ArgumentNullException(nameof(@this));
        //     if (switchTo == null) throw new ArgumentNullException(nameof(switchTo));
        //     return @this.SelectMany(entry => entry != default(T) ? entry.ReturnObservable() : switchTo);
        // }

        // [UsedImplicitly]
        // public static IObjectSpaceAsync ToAsynch<T>(this T objectSpace) where T : IObjectSpace => (IObjectSpaceAsync)objectSpace;
        // public static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters){
        //     dynamic awaitable = @this.Invoke(obj, parameters);
        //     await awaitable;
        //     return awaitable.GetAwaiter().GetResult();
        // }

        // [PublicAPI]
        // public static IObservable<T> To<T>(this IObservable<object> source) => source.Select(o => (T)typeof(T).CreateInstance());

        // [UsedImplicitly]
        // public static IDisposable SubscribeSafe<T>(this IObservable<T> source) => source.HandleErrors().Subscribe();

        // public static SmtpClient NewSmtpClient(this NameValueCollection appSettings){
        //     const string reportEnableSsl = nameof(reportEnableSsl);
        //     const string reportEmailPass = nameof(reportEmailPass);
        //     const string reportEmail = nameof(reportEmail);
        //     const string reportEmailPort = nameof(reportEmailPort);
        //     const string reportEmailserver = nameof(reportEmailserver);
        //
        //     var smtpClient = new SmtpClient(appSettings[reportEmailserver]){
        //         Port = Convert.ToInt32(appSettings[reportEmailPort]),
        //         DeliveryMethod = SmtpDeliveryMethod.Network,
        //         UseDefaultCredentials = false,
        //         Credentials = new NetworkCredential(appSettings[reportEmail],
        //             appSettings[reportEmailPass]),
        //         EnableSsl = Convert.ToBoolean(appSettings[reportEnableSsl])
        //     };
        //     return smtpClient;
        // }

        // public static IObservable<T> HandleErrors<T>(this IObservable<T> source, Func<System.Exception, IObservable<T>> exceptionSelector = null) => source.Catch<T, System.Exception>(exception => {
        //     throw new NotImplementedException();
        //     // if (Tracing.IsTracerInitialized) Tracing.Tracer.LogError(exception);
        //     // return Observable.Using(ConfigurationManager.AppSettings.NewSmtpClient, smtpClient =>
        //     // {
        //     //     var errorMail = exception.ToMailMessage(((NetworkCredential)smtpClient.Credentials).UserName);
        //     //     try
        //     //     {
        //     //         smtpClient.Send(errorMail);
        //     //     }
        //     //     catch (Exception e)
        //     //     {
        //     //         if (Tracing.IsTracerInitialized) Tracing.Tracer.LogError(e);
        //     //     }
        //     //     return exception.Handle(exceptionSelector);
        //     // });
        // });

        // public static IObservable<T> Handle<T>(this System.Exception exception, Func<System.Exception, IObservable<T>> exceptionSelector = null) => exception is WarningException ? default(T).ReturnObservable() :
                // exceptionSelector != null ? exceptionSelector(exception) : Observable.Throw<T>(exception);

        // public static IObservable<T> SubscribePublish<T>(this IObservable<T> source){
        //     var publish = source.Publish();
        //     publish.Connect();
        //     return publish;
        // }

        // public static System.DateTime UnixTimestampToDateTimeMilliSecond(this double unixTime) => UnixTimestampToDateTime(unixTime, TimeSpan.TicksPerMillisecond);
        //
        // private static System.DateTime UnixTimestampToDateTime(double unixTime, long ticks)
        // {
        //     var unixStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        //     var unixTimeStampInTicks = (long)(unixTime * ticks);
        //     return new System.DateTime(unixStart.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
        // }
        //
        // public static double UnixTimestampFromDateTimeMilliseconds(this System.DateTime dateTime) => (dateTime - new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        // public static IObservable<T> WhenModifiedObjects<T>(this IObjectSpace objectSpace, bool emitAfterCommit = false, ObjectModification objectModification = ObjectModification.All) => objectSpace.WhenCommiting().SelectMany(_ =>
        // {
        //     var modifiedObjects = objectSpace.ModifiedObjects<T>(objectModification).ToArray();
        //     return emitAfterCommit ? objectSpace.WhenCommited().FirstAsync().SelectMany(pattern => modifiedObjects) : modifiedObjects.ToObservable();
        // });
        // [PublicAPI]
        // public static IEnumerable<T> ModifiedObjects<T>(this IObjectSpace objectSpace, ObjectModification objectModification) => objectSpace.ModifiedObjects.Cast<object>().Select(o => o).OfType<T>().Where(_ =>
        // {
        //     if (objectModification == ObjectModification.Deleted)
        //     {
        //         return objectSpace.IsDeletedObject(_);
        //     }
        //     if (objectModification == ObjectModification.New)
        //     {
        //         return objectSpace.IsNewObject(_);
        //     }
        //     if (objectModification == ObjectModification.Updated)
        //     {
        //         return objectSpace.IsUpdated(_);
        //     }
        //     if (objectModification == ObjectModification.NewOrDeleted)
        //     {
        //         return objectSpace.IsNewObject(_) || objectSpace.IsDeletedObject(_);
        //     }
        //     if (objectModification == ObjectModification.NewOrUpdated)
        //     {
        //         return objectSpace.IsNewObject(_) || objectSpace.IsUpdated(_);
        //     }
        //     if (objectModification == ObjectModification.DeletedOrUpdated)
        //     {
        //         return objectSpace.IsUpdated(_) || objectSpace.IsDeletedObject(_);
        //     }
        //
        //     return true;
        //
        // });

        // [DebuggerStepThrough]
        // public static UnitOfWork UnitOfWork(this IObjectSpace objectSpace) => (UnitOfWork)((XPObjectSpace)objectSpace).Session;

        // static bool IsUpdated<T>(this IObjectSpace objectSpace, T t) => !objectSpace.IsNewObject(t) && !objectSpace.IsDeletedObject(t);

        // public static string FirstCharacterToLower(this string str) => string.IsNullOrEmpty(str) || char.IsLower(str, 0) ? str : char.ToLowerInvariant(str[0]) + str.Substring(1);

        // [PublicAPI]
        // public static IObservable<T> DeleteObjectSpaceLink<T>(this IObservable<T> source) where T : IObjectSpaceLink => source.Select(link =>
        // {
        //     link.ObjectSpace.Delete(link);
        //     link.ObjectSpace.CommitChanges();
        //     return link;
        // });

        // [PublicAPI]
        // public static IObservable<T> WhenDeletedObjects<T>(this IObjectSpace objectSpace, bool emitAfterCommit = false) => emitAfterCommit ? objectSpace.WhenModifiedObjects<T>(true, ObjectModification.Deleted)
        //         : objectSpace.WhenObjectDeleted()
        //             .SelectMany(pattern => pattern.EventArgs.Objects.Cast<object>()).OfType<T>()
        //             .TakeUntil(objectSpace.WhenDisposed());
        // [PublicAPI]
        // public static IObservable<EventPattern<ObjectsManipulatingEventArgs>> WhenObjectDeleted(this IObjectSpace objectSpace) => Observable.FromEventPattern<EventHandler<ObjectsManipulatingEventArgs>, ObjectsManipulatingEventArgs>(
        //             h => objectSpace.ObjectDeleted += h, h => objectSpace.ObjectDeleted -= h, RegularEventsScheduler)
        //         .TakeUntil(objectSpace.WhenDisposed());
    }


    // public enum ObjectModification
    // {
    //     All,
    //     New,
    //     Deleted,
    //     Updated,
    //     NewOrDeleted,
    //     NewOrUpdated,
    //     DeletedOrUpdated
    // }
}