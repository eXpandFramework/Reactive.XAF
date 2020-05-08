using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using DevExpress.ExpressApp;
using JetBrains.Annotations;
using Microsoft.Identity.Client;

namespace Xpand.Extensions.Office.Cloud.Microsoft{
    public static class TokenCacheExtensions{

        private static IObservable<TokenCacheNotificationArgs> WriteStorage(this ITokenCache tokenCache, string cacheFilePath) => tokenCache.AfterAccess()
            .Select(args => {
                var bytes = ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(cacheFilePath, bytes);
                return args;
            });

        private static IObservable<TokenCacheNotificationArgs> WriteStorage(this ITokenCache tokenCache, Func<IObjectSpace> objectspaceFactory, Guid userId) 
            => tokenCache.AfterAccess().Select(args => {
                using (var objectSpace = objectspaceFactory()){
                    var authentication = objectSpace.GetObjectByKey<MSAuthentication>(userId) ?? objectSpace.CreateObject<MSAuthentication>();
                    authentication.Oid = userId;
                    authentication.Token = args.TokenCache.SerializeMsalV3();
                    objectSpace.CommitChanges();
                }
                return args;
            });

        public static IObservable<TokenCacheNotificationArgs> SynchStorage(this ITokenCache tokenCache, Func<IObjectSpace> objectspaceFactory, Guid userId) 
            => tokenCache.ReadStorage(objectspaceFactory, userId).Merge(tokenCache.WriteStorage(objectspaceFactory, userId));
        [UsedImplicitly]
        public static IObservable<TokenCacheNotificationArgs> SynchStorage(this ITokenCache tokenCache, string cacheFilePath) 
            => tokenCache.ReadStorage(cacheFilePath).Merge(tokenCache.WriteStorage(cacheFilePath));

        private static IObservable<TokenCacheNotificationArgs> ReadStorage(this ITokenCache tokenCache, Func<IObjectSpace> objectspaceFactory, Guid userId) 
            => tokenCache.BeforeAccess().Select(args => {
                using (var objectSpace = objectspaceFactory()){
                    var authentication = objectSpace.GetObjectByKey<MSAuthentication>(userId) ?? objectSpace.CreateObject<MSAuthentication>();
                    authentication.Oid = userId;
                    args.TokenCache.DeserializeMsalV3(authentication.Token);
                    objectSpace.CommitChanges();
                }
                return args;
            });

        private static IObservable<TokenCacheNotificationArgs> ReadStorage(this ITokenCache tokenCache, string cacheFilePath) => tokenCache.BeforeAccess()
            .Select(args => {
                var bytes = File.Exists(cacheFilePath) ? ProtectedData.Unprotect(File.ReadAllBytes(cacheFilePath), null,
                    DataProtectionScope.CurrentUser) : null;
                args.TokenCache.DeserializeMsalV3(bytes);
                return args;
            });
        
        private static IObservable<TokenCacheNotificationArgs> BeforeAccess(this ITokenCache tokenCache){
            
            var subject = new Subject<TokenCacheNotificationArgs>();
            tokenCache.SetBeforeAccess(args => subject.OnNext(args));
            return subject.AsObservable();
        }

        private static IObservable<TokenCacheNotificationArgs> AfterAccess(this ITokenCache tokenCache){
            var subject = new Subject<TokenCacheNotificationArgs>();
            tokenCache.SetAfterAccess(args => subject.OnNext(args));
            return subject.AsObservable().Where(args => args.HasStateChanged);
        }
    }
}