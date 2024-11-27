using System;
using System.Collections.Generic;
using System.Configuration;
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
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.StoreToDisk{
    public static class StoreToDiskService{
        internal static IObservable<Unit> Connect(this XafApplication application) {
            
            return application.WhenSetupComplete()
                .SelectMany(_ => application.StoreToDisk());
        }

        private static IObservable<UnitOfWork> LoadFromDisk(
            this (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) source,
            ((XPClassInfo classInfo, ITypeInfo typeInfo,bool autoCreate) types, (IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, ThreadSafeDataLayer layer, string Criteria) data) 
            => Observable.Defer(() => {
                var newObjects = source.details.Where(details => details.modification == ObjectModification.New).Select(t => t.instance)
                    .Where(o => source.objectSpace.IsObjectFitForCriteria(data.Criteria,o)).ToArray();
                var unitOfWork = new UnitOfWork(data.layer);
                newObjects.Execute(newObject => {
                    var id = data.key.keyMember.GetValue(newObject);
                    var savedObject = unitOfWork.GetObjectByKey(data.types.classInfo, id);
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
                        // .Where(t => t.types.typeInfo.Type.Name=="LaunchPadProjectType")
                    .SelectMany(t => application.WhenProviderCommittingDetailed(t.types.typeInfo.Type,ObjectModification.NewOrUpdated,true,[]).Where(details => details.details.Length>0)
                        .SelectMany(committed => {
                            var modifiedObjects = committed.objectSpace.ModifiedObjects(ObjectModification.NewOrUpdated)
                                .Select(t1 => t1.instance).ExactType(t.types.typeInfo.Type).Where(o => t.Criteria==null||committed.objectSpace.IsObjectFitForCriteria(t.Criteria,o))
                                .ToArray();
                            return committed.LoadFromDisk((t.types,t.key,t.memberInfos,data.layer,t.Criteria)).Zip(committed.objectSpace.WhenCommitted().Take(1)).ToFirst()
                                .TakeUntil(committed.objectSpace.WhenDisposed().MergeToUnit(committed.objectSpace.WhenRollingBack()))
                                .SelectMany(unitOfWork => unitOfWork.SaveData(modifiedObjects,(t.key,t.memberInfos,t.types)));
                        }))
                    .MergeToUnit(application.AutoCreate( data))
                )
                .ToUnit();

        private static IObservable<object> AutoCreate(this XafApplication application,
            (ThreadSafeDataLayer layer, ((XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data) data) {
            return data.data.Where(t => t.types.autoCreate).ToNowObservable().BufferUntilCompleted()
                .Select(ts => ts.Select(t => t.types.typeInfo).ToList().SortByDependencies()
                    .Select(info => ts.First(t => t.types.typeInfo==info)))
                .SelectMany(ts => application.WhenLoggedOn().Take(1)
                    .SelectMany(_ => ts.ToObservable().SelectMany(t => application.UseProviderObjectSpace(objectSpace => 
                        objectSpace.GetObjectsCount(t.types.typeInfo.Type, null) == 0 ? new UnitOfWork(data.layer)
                                .Use(unitOfWork => new XPCollection(unitOfWork, t.types.classInfo).Cast<object>().ToNowObservable()
                                    .Do(storedObject => objectSpace.EnsureObjectByKey(t.types.typeInfo.Type, unitOfWork.GetKeyValue(storedObject)))
                                    .FinallySafe(objectSpace.CommitChanges))
                            : Observable.Empty<object>(), t.types.typeInfo.Type))));
        }

        public static IList<ITypeInfo> SortByDependencies(this IList<ITypeInfo> typeInfos) {
            var dependencyMap = typeInfos.ToDictionary(type => type,
                type => type.Members.Where(info => info.IsPersistent).Select(info => info.MemberTypeInfo).Where(typeInfos.Contains).ToList()
            );

            var infos = new List<ITypeInfo>();
            var infosCount = typeInfos.Count;
            while (infos.Count<infosCount) {
                var typeInfo = typeInfos.FirstOrDefault(info => !dependencyMap[info].Contains(info));
                var index = infos.FindIndex(info => dependencyMap[info].Contains(typeInfo));
                if (index >= 0) {
                    infos.Insert(index, typeInfo);
                }
                else {
                    infos.Add(typeInfo);    
                }
                typeInfos.Remove(typeInfo);
            }

            return infos;
        }

        private static IObservable<Unit> SaveData(this UnitOfWork unitOfWork, object[] modifiedObjects,
            ((IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, (XPClassInfo classInfo, ITypeInfo typeInfo,bool autoCreate) types) t)
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
                .DoWhen((i, _) => i%100==0,(_, _) => unitOfWork.CommitChanges())
                .FinallySafe(() => {
                    unitOfWork.CommitChanges();
                    unitOfWork.Dispose();
                })
                .ToUnit();

        private static
            IObservable<(ThreadSafeDataLayer layer, ((XPClassInfo classInfo, ITypeInfo typeInfo,bool autoCreate) types,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data)> StoreToDiskData(this XafApplication application) {
            var reflectionDictionary = new ReflectionDictionary();
            return application.TypesInfo.PersistentTypes
                .Select(info => info).Attributed<StoreToDiskAttribute>().ToNowObservable()
                .Validate()
                // .Where(t => t.attribute.AutoCreate)
                .Select(t => {
                    var classInfo = reflectionDictionary.CreateClass(t.typeInfo.Type.Name,new DeferredDeletionAttribute(false),new OptimisticLockingAttribute(OptimisticLockingBehavior.NoLocking));
                    var memberInfos = t.MemberInfos( classInfo);
                    var keyMember = t.typeInfo.FindMember(t.attribute.Key??t.typeInfo.KeyMember.Name);
                    if (keyMember.IsAutoGenerate) throw new Exception($"Auto generate key member {t.typeInfo}");
                    var storeToDiskKeyMember = classInfo.CreateMember(keyMember.Name, keyMember.MemberType, keyMember.Attributes.Where(attribute => attribute.GetType()!=typeof(KeyAttribute))
                            .AddItem(new KeyAttribute()).ToArray());
                    return ((types:classInfo,t.typeInfo,t.attribute.AutoCreate),memberInfos, key: (keyMember, storeToDiskKeyMember), t.attribute.Criteria);

                }).BufferUntilCompleted()
                .SelectMany(data => reflectionDictionary.DataLayer()
                    .Select(layer => (layer, data)));
        }

        private static Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo xpCustomMemberInfo)> MemberInfos(this (StoreToDiskAttribute attribute, ITypeInfo typeInfo) t, XPClassInfo classInfo){
            var properties = (!t.attribute.Map ? t.attribute.Properties
                : t.typeInfo.Members.Where(info => info.IsPersistent && !info.IsService && !info.IsKey)
                    .Select(info => info.Name)).Distinct();
            var memberInfos = properties.Select(property => t.typeInfo.FindMember(property))
                .Select(memberInfo => (memberInfo, xpCustomMemberInfo: classInfo.CreateMember(memberInfo.Name,
                    memberInfo.MemberTypeInfo.IsPersistent ? memberInfo.MemberTypeInfo.KeyMember.MemberType
                        : memberInfo.MemberType, memberInfo.Attributes.ToArray())))
                .ToDictionary(t1 => t1.memberInfo.Name, t1 => t1);
            return memberInfos;
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