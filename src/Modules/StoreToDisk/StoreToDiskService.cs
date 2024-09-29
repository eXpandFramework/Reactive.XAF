using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo;
using Xpand.Extensions.XAF.Xpo.ConnectionProviders;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.StoreToDisk{
    public static class StoreToDiskService{
        internal static IObservable<Unit> Connect(this XafApplication application) {
            
            return application.WhenSetupComplete()
                .SelectMany(_ => application
                    .StoreToDisk()
                    .MergeToUnit(application.DailyBackup()));
        }
        
        private static IObservable<Unit> DailyBackup(this XafApplication application)
            => application.WhenLoggedOn()
                .Select(_ => application.Model.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk)
                .Where(storeToDisk => storeToDisk.DailyBackup&&Directory.Exists(storeToDisk.Folder)).ObserveOnDefault()
                .SelectMany(disk => {
                    if (!Directory.Exists(disk.Folder)) return Observable.Empty<string>();
                    var jsonFles = Directory.GetFiles(disk.Folder, "*.json");
                    var directoryName = $"{disk.Folder}\\{DateTime.Now:yyyy.MM.dd}";
                    if (!jsonFles.Any() || Directory.Exists(directoryName)) return Observable.Empty<string>();
                    Directory.CreateDirectory(directoryName);
                    return jsonFles.Execute(file => File.Copy(file, Path.Combine(directoryName,Path.GetFileName(file)))).ToNowObservable();
                })
                .ToUnit();

        private static IObservable<UnitOfWork> LoadFromDisk(
            this (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) source,
            ((XPClassInfo classInfo, ITypeInfo typeInfo) types, (IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, ThreadSafeDataLayer layer, string Criteria) data) 
            => Observable.Defer(() => {
                var newObjects = source.details.Where(details => details.modification == ObjectModification.New).Select(t => t.instance)
                    .Where(o => source.objectSpace.IsObjectFitForCriteria(data.Criteria,o)).ToArray();
                var unitOfWork = new UnitOfWork(data.layer);
                newObjects.Execute(newObject => {
                    var savedObject = unitOfWork.GetObjectByKey(data.types.classInfo, data.key.keyMember.GetValue(newObject));
                    if (savedObject != null) {
                        data.memberInfos.Values.Select(t => t.xpCustomMemberInfo).Execute(xpCustomMemberInfo
                                => {
                                var memberInfo = data.memberInfos[xpCustomMemberInfo.Name].memberInfo;
                                var value = xpCustomMemberInfo.GetValue(savedObject);
                                if (memberInfo.MemberTypeInfo.IsPersistent) {
                                    value = source.objectSpace.GetObjectByKey(memberInfo.MemberTypeInfo.Type, value);
                                }
                                memberInfo.SetValue(newObject, value);
                            })
                            .Enumerate();
                    }
                }).Enumerate();
                return (unitOfWork).Observe();
            });


        public static IObservable<Unit> StoreToDisk(this XafApplication application)
            => application.StoreToDiskData()
                .SelectMany(data => data.data.ToNowObservable()
                    // .Where(t => t.types.typeInfo.Type.Name=="Network")
                    .SelectMany(t => application.WhenProviderCommittingDetailed(t.types.typeInfo.Type,ObjectModification.NewOrUpdated,true,[]).Where(details => details.details.Length>0)
                    .SelectMany(committed => {
                        var modifiedObjects = committed.objectSpace.ModifiedObjects(ObjectModification.NewOrUpdated)
                            .Select(t1 => t1.instance).ExactType(t.types.typeInfo.Type).Where(o => t.Criteria==null||committed.objectSpace.IsObjectFitForCriteria(t.Criteria,o))
                            .ToArray();
                        return committed.LoadFromDisk((t.types,t.key,t.memberInfos,data.layer,t.Criteria)).Zip(committed.objectSpace.WhenCommitted().Take(1)).ToFirst()
                            .TakeUntil(committed.objectSpace.WhenDisposed().MergeToUnit(committed.objectSpace.WhenRollingBack()))
                            .SelectMany(unitOfWork => unitOfWork.SaveData(modifiedObjects,(t.key,t.memberInfos,t.types)));
                    })))
                
                .ToUnit();

        private static IObservable<Unit> SaveData(this UnitOfWork unitOfWork, object[] modifiedObjects,
            ((IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, (XPClassInfo classInfo, ITypeInfo typeInfo) types) t)
            => modifiedObjects.ToNowObservable().SelectMany(modifiedObject => {
                    var key = t.key.keyMember.GetValue(modifiedObject);
                    object storedToDiskObject = null;
                    return t.memberInfos.Values
                        .Select(members => {
                            var theValue = members.memberInfo.GetValue(modifiedObject);
                            if (theValue.IsDefaultValue(members.memberInfo.MemberType)) return Unit.Default;
                            if ( storedToDiskObject == null) {
                                storedToDiskObject=unitOfWork.GetObjectByKey(t.types.classInfo,key)?? t.types.classInfo.CreateObject(unitOfWork);
                                t.key.storeToDiskKeyMember.SetValue(storedToDiskObject,key);
                            }
                            members.xpCustomMemberInfo.SetValue(storedToDiskObject, theValue);

                            return Unit.Default;
                        });
                })
                .DoWhen((i, unit) => i%100==0,(unit, i) => unitOfWork.CommitChanges())
                .FinallySafe(() => {
                    unitOfWork.CommitChanges();
                    unitOfWork.Dispose();
                })
                .ToUnit();

        private static
            IObservable<(ThreadSafeDataLayer layer, ((XPClassInfo classInfo, ITypeInfo typeInfo) types,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data)>
            StoreToDiskData(this XafApplication application) {
            var reflectionDictionary = new ReflectionDictionary();
            return application.TypesInfo.PersistentTypes
                .Select(info => info).Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Validate()
                .Select(t => {
                    var classInfo = reflectionDictionary.CreateClass(t.typeInfo.Type.Name,new DeferredDeletionAttribute(false),new OptimisticLockingAttribute(OptimisticLockingBehavior.NoLocking));
                    var memberInfos = t.attribute.Properties.Select(property => t.typeInfo.FindMember(property))
                        .Select(memberInfo => (memberInfo, xpCustomMemberInfo: classInfo.CreateMember(memberInfo.Name,
                            memberInfo.MemberTypeInfo.IsPersistent
                                ? memberInfo.MemberTypeInfo.KeyMember.MemberType
                                : memberInfo.MemberType, memberInfo.Attributes.ToArray())))
                        .ToDictionary(t1 => t1.memberInfo.Name, t1 => t1);
                    var keyMember = t.typeInfo.FindMember(t.attribute.Key);
                    var storeToDiskKeyMember =
                        classInfo.CreateMember(keyMember.Name, keyMember.MemberType, keyMember.Attributes.Where(attribute => attribute.GetType()!=typeof(KeyAttribute))
                            .AddItem(new KeyAttribute()).ToArray());
                    return ((types:classInfo,t.typeInfo),memberInfos, key: (keyMember, storeToDiskKeyMember), t.attribute.Criteria);

                }).BufferUntilCompleted()
                .SelectMany(data => reflectionDictionary.DataLayer()
                    .Select(layer => (layer, data)));
        }

        private static IObservable<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)> Validate(
            this IObservable<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)> source)
            => source.If(t => t.typeInfo.IsAbstract, t
                    => new InvalidOperationException(
                            $"{nameof(StoreToDiskAttribute)} found on abstract {t.typeInfo.FullName}")
                        .Throw<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)>(), t => t.Observe())
                .If(t => !t.typeInfo.IsPersistent,
                    t => new InvalidOperationException(
                            $"{nameof(StoreToDiskAttribute)} found on NonPersistent {t.typeInfo.FullName}")
                        .Throw<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)>(), t => t.Observe());
        private static IObservable<ThreadSafeDataLayer> DataLayer(this ReflectionDictionary reflectionDictionary)
            => AppDomain.CurrentDomain.ExecuteOnce()
                .Select(_ => {
                    var connectionString = ConfigurationManager.ConnectionStrings["StoreToDisk"].ConnectionString;
                    reflectionDictionary.UpdateSchema( connectionString);
                    var cachedDataStoreProvider = new CachedDataStoreProvider(connectionString);
                    return new ThreadSafeDataLayer(reflectionDictionary, cachedDataStoreProvider
                        .CreateWorkingStore(out var _));
                });

        
    }
}