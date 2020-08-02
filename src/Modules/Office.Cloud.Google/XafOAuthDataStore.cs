using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Google.Apis.Json;
using Google.Apis.Util.Store;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;

namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public class XafOAuthDataStore : IDataStore{
        private readonly Func<IObjectSpace> _objectSpaceFactory;
        private readonly Guid _userId;

        public XafOAuthDataStore(Func<IObjectSpace> objectSpaceFactory, Guid userId, Platform platform){
            Platform = platform;
            _objectSpaceFactory = objectSpaceFactory;
            _userId = userId;
        }

        public Platform Platform{ get; }

        Task IDataStore.StoreAsync<T>(string key, T value) 
            => Observable.Using(_objectSpaceFactory, objectSpace => {
                key = FileDataStore.GenerateStoredKey(key, typeof(T));
                var cloudAuthentication = objectSpace.GetObjectByKey<GoogleAuthentication>(_userId) ??
                                          objectSpace.CreateObject<GoogleAuthentication>();
                cloudAuthentication.Oid = _userId;
                var serialize = NewtonsoftJsonSerializer.Instance.Serialize(value);
                if (!cloudAuthentication.OAuthToken.ContainsKey(key))
                    cloudAuthentication.OAuthToken.Add(key, serialize);
                else
                    cloudAuthentication.OAuthToken[key] = serialize;
                cloudAuthentication.Save();
                objectSpace.CommitChanges();
                return Observable.Return(default(T));
            }).ToTask();

        Task IDataStore.DeleteAsync<T>(string key) 
            => Observable.Using(_objectSpaceFactory, objectSpace => {
                objectSpace.Delete(objectSpace.GetObjectByKey<GoogleAuthentication>(_userId));
                objectSpace.CommitChanges();
                return Unit.Default.ReturnObservable();
            }).ToTask();

        Task<T> IDataStore.GetAsync<T>(string key) 
            => Observable.Using(_objectSpaceFactory, objectSpace => {
                    key = FileDataStore.GenerateStoredKey(key, typeof(T));
                    var cloudAuthentication = objectSpace.GetObjectByKey<GoogleAuthentication>(_userId);
                    return (cloudAuthentication != null && cloudAuthentication.OAuthToken.ContainsKey(key)
                        ? cloudAuthentication.OAuthToken[key]
                        : null).ReturnObservable();
                })
                .Select(s => NewtonsoftJsonSerializer.Instance.Deserialize<T>(s))
                .ToTask();

        Task IDataStore.ClearAsync() 
            => Observable.Using(_objectSpaceFactory, objectSpace => {
                var cloudAuthentication = objectSpace.GetObjectByKey<GoogleAuthentication>(_userId);
                cloudAuthentication.OAuthToken.Clear();
                objectSpace.CommitChanges();
                return Unit.Default.ReturnObservable();
            }).ToTask();
    }
}