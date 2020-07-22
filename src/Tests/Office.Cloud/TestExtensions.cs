using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Shouldly;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Office.Cloud.Tests{
	public static class TestExtensions{
        
        public static async Task Populate_All<TEntity>(this IObjectSpace objectSpace, string syncToken,
            Func<CloudOfficeTokenStorage,IObservable<TEntity>> listEvents,TimeSpan timeout,Action<IObservable<TEntity>> assert,IObjectSpaceProvider assertTokenStorageProvider=null){
            
            var tokenStorage = objectSpace.CreateObject<CloudOfficeTokenStorage>();
            tokenStorage.Token = syncToken;
            tokenStorage.EntityName = typeof(TEntity).FullName;
            objectSpace.CommitChanges();
            var storageOid = tokenStorage.Oid;
            var events = listEvents(tokenStorage).SubscribeReplay();
                
            await events.Timeout(timeout);

            if (assertTokenStorageProvider!=null){
                using (var space = assertTokenStorageProvider.CreateObjectSpace()){
                    tokenStorage = space.GetObjectsQuery<CloudOfficeTokenStorage>()
                        .First(storage => storage.Oid == storageOid);
                    tokenStorage.Token.ShouldNotBeNull();
                    tokenStorage.Token.ShouldNotBe(syncToken);
                }
            }

            assert(events);

        }

        public static async Task Delete_Two_Entities<TCloudEntity,TLocalEntity>(this IObjectSpace objectSpace,
            TLocalEntity[] localEntites, Func<IObjectSpace, IObservable<TCloudEntity>> synchronize,
            Func<Task> assert,TimeSpan timeout,TCloudEntity[] existingEntities){
            
            var localEntity1 = objectSpace.GetObject(localEntites[0]);
            var cloudEntity1 = existingEntities.First();
            await objectSpace.NewCloudObject(localEntity1, cloudEntity1);
            var localEntity2 = objectSpace.GetObject(localEntites[1]);
            var cloudEntity2 = existingEntities.Skip(1).First();
            await objectSpace.NewCloudObject(localEntity2, cloudEntity2);

            var map = synchronize(objectSpace).SubscribeReplay();
            objectSpace.Delete(localEntity1);
            objectSpace.CommitChanges();
            objectSpace.Delete(localEntity2);
            objectSpace.CommitChanges();

            
            await map.Take(2).SelectMany((cloudEntity, i) => Observable.FromAsync(assert)).Timeout(timeout);

        }

        public static async Task Map_Existing_Entity_Two_Times<TCloudEntity, TLocalEntity>(this IObjectSpace objectSpace, TLocalEntity localEntity,
            Action<TLocalEntity, int> modifyLocalEntity, TCloudEntity newCloudEntity, Func<IObjectSpace, IObservable<TCloudEntity>> synchronize,
            Func<TLocalEntity, TCloudEntity,Task> assert, TimeSpan timeout) {
            
            
            localEntity = objectSpace.GetObject(localEntity);
            
            await objectSpace.NewCloudObject(localEntity, newCloudEntity).ToTaskWithoutConfigureAwait();
            var map = synchronize(objectSpace).SubscribePublish();

            
            modifyLocalEntity(localEntity,0);
            objectSpace.CommitChanges();
            
            await map.Take(1).SelectMany((cloudEntity, i) => assert( localEntity, cloudEntity).ToObservable())
                .Timeout(timeout).ToTaskWithoutConfigureAwait();
            
            modifyLocalEntity(localEntity,1);
            objectSpace.CommitChanges();
            
            await map.Take(1).Select((cloudEntity, i) => {
                    assert( localEntity, cloudEntity);
                    return Unit.Default;
                })
                .Timeout(timeout);
        }

        

        public static async Task Map_Two_New_Entity<TCloudEntity,TLocalEntity>(this IObjectSpaceProvider objectSpaceProvider,Func<IObjectSpace,TLocalEntity> localEntityFactory,TimeSpan timeout,
            Func<IObjectSpace, IObservable<TCloudEntity>> synchronize, Action<TLocalEntity,TCloudEntity> assert){
            

            var objectSpace = objectSpaceProvider.CreateObjectSpace();
            var map = synchronize(objectSpace).SubscribeReplay();
            var localEntity1 = localEntityFactory(objectSpace);
            objectSpace.CommitChanges();
            var localEntity2 = localEntityFactory(objectSpace);
            objectSpace.CommitChanges();

            await map.Take(2).Select((cloudEntity, i) => {
                var localEntity = i == 0 ? localEntity1 : localEntity2;
                assert( localEntity, cloudEntity);
                return Unit.Default;
            }).Timeout(timeout);
            objectSpace.Dispose();
        }
        public static async Task Map_Two_New_Entity<TCloudEntity,TLocalEntity>(this IObjectSpace objectSpace,Func<IObjectSpace,int,TLocalEntity> localEntityFactory,TimeSpan timeout,
            Func<IObjectSpace, IObservable<TCloudEntity>> synchronize, Action<TLocalEntity,TCloudEntity,int> assert){
            
            var map = synchronize(objectSpace).Take(2).SubscribeReplay();
            var localEntity1 = localEntityFactory(objectSpace,0);
            objectSpace.CommitChanges();
            
            await map.FirstAsync().Select((cloudEntity, i) => {
                    assert( localEntity1, cloudEntity,0);
                    return Unit.Default;
                })
                .TakeUntil(objectSpace.WhenDisposed())
                .Timeout(timeout);

            // map = synchronize(objectSpace).FirstAsync().SubscribeReplay();
            var localEntity2 = localEntityFactory(objectSpace,1);
            objectSpace.CommitChanges();

            await map.LastAsync().Select((cloudEntity, i) => {
                    assert( localEntity2, cloudEntity,1);
                    return Unit.Default;
                })
                .TakeUntil(objectSpace.WhenDisposed())
                .Timeout(timeout);
            

            
            
        }

    }
}
