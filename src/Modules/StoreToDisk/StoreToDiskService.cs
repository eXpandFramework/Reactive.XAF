using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Exceptions;
using DevExpress.Xpo.Metadata;
using HarmonyLib;
using Swordfish.NET.Collections.Auxiliary;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo;
using Xpand.Extensions.XAF.Xpo.ConnectionProviders;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.StoreToDisk{
    public interface IStoreToDiskAutoCreate {
        IObservable<Unit> WhenObjectsReady();
    }
    public static class StoreToDiskService{
        internal static IObservable<Unit> Connect(this XafApplication application) {
            
            return application.WhenSetupComplete()
                .SelectMany(_ => application.StoreToDisk());
        }

        private static IObservable<UnitOfWork> LoadFromDisk(
            this (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) source,
            ((XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types, (IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos,
                ThreadSafeDataLayer layer, string Criteria) data) 
            => Observable.Defer(() => {
                var newObjects = source.details.Where(details => details.modification == ObjectModification.New).Select(t => t.instance)
                    .Where(o => source.objectSpace.IsObjectFitForCriteria(data.Criteria,o)).ToArray();
                var unitOfWork = new UnitOfWork(data.layer);
                newObjects.Execute(newObject => {
                    var id = data.key.keyMember.GetValue(newObject);
                    var storedObject = unitOfWork.GetObjectByKey(data.types.classInfo, id);
                    if (storedObject != null) {
                        data.memberInfos.Keys.Execute(key => source.SetValue( data, key, storedObject, newObject)).Enumerate();
                    }
                }).Enumerate();
                return (unitOfWork).Observe();
            });

        private static void SetValue(this (IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details) source,
            ((XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types, (IMemberInfo keyMember, XPCustomMemberInfo
                storeToDiskKeyMember) key, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, ThreadSafeDataLayer layer, string Criteria) data, string key,
            object storedObject, object newObject){
            var dataMemberInfo = data.memberInfos[key];
            var memberInfo = dataMemberInfo.memberInfo;
            object value = null;
            if (dataMemberInfo.xpCustomMemberInfos.Length == 1) {
                value = dataMemberInfo.xpCustomMemberInfos.First().GetValue(storedObject);
                if (memberInfo.MemberTypeInfo.IsPersistent) {
                    value = source.objectSpace.GetObjectByKey(memberInfo.MemberTypeInfo.Type, value);
                }
                
            }
            else {
                var criteria = dataMemberInfo.xpCustomMemberInfos.Select(info => $"{info.Name}=?").Join(" AND ");
                var parameters = dataMemberInfo.xpCustomMemberInfos.Select(info => info.GetValue(storedObject)).WhereNotDefault().ToArray();
                if (parameters.Length > 0) {
                    value=source.objectSpace.FindObject(memberInfo.MemberTypeInfo.Type, CriteriaOperator.Parse(criteria,parameters));    
                }
            }
            
            memberInfo.SetValue(newObject, value);
            
        }


        public static IObservable<Unit> StoreToDisk(this XafApplication application)
            => application.StoreToDiskData()
                .SelectMany(data => data.data.ToNowObservable()
                    // .Where(t => _types.Contains(t.types.typeInfo.Type.Name))
                    .SelectMany(t => application.WhenProviderCommittingDetailed(t.types.typeInfo.Type,ObjectModification.NewOrUpdated,true,[]).Where(details => details.details.Length>0)
                        .SelectMany(committed => {
                            var modifiedObjects = committed.objectSpace.ModifiedObjects(ObjectModification.NewOrUpdated)
                                .Select(t1 => t1.instance).ExactType(t.types.typeInfo.Type).Where(o => t.Criteria==null||committed.objectSpace.IsObjectFitForCriteria(t.Criteria,o))
                                .ToArray();
                            return committed.LoadFromDisk((t.types,t.key,t.memberInfos,data.layer,t.Criteria)).Zip(committed.objectSpace.WhenCommitted().Take(1)).ToFirst()
                                .TakeUntil(committed.objectSpace.WhenDisposed().MergeToUnit(committed.objectSpace.WhenRollingBack()))
                                // .Where(_ => _types.Contains(t.types.typeInfo.Type.Name))
                                .SelectMany(unitOfWork => unitOfWork.SaveData(modifiedObjects,(t.key,t.memberInfos,t.types)));
                        })
                    )
                    .MergeToUnit(application.AutoCreate( data))
                )
                .ToUnit();

        static IObservable<Unit> AutoCreate(this XafApplication application,
            (ThreadSafeDataLayer layer, ((XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data) data) {
            return data.data.Where(t => t.types.autoCreate).ToNowObservable().BufferUntilCompleted()
                .Select(ts => ts.Select(t => t.types.typeInfo).ToList().SortByDependencies()
                    .Select(info => ts.First(t => t.types.typeInfo==info)))
                // .ConcatIgnored(_ => application.ObjectSpaceProviders.Where(provider => provider is not NonPersistentObjectSpaceProvider)
                //     .ToNowObservable().Do(provider => provider.UpdateSchema()))
                .ConcatIgnored(tuples => application.ObjectSpaceProviders.Where(provider => provider is not NonPersistentObjectSpaceProvider)
                    .ToNowObservable().SelectMany(provider => provider.WhenSchemaUpdated().Take(1)))
                .SelectMany(ts => {
                    // var selectMany = application.Modules.OfType<IStoreToDiskAutoCreate>().ToNowObservable()
                    //     .SelectMany(create => create.WhenObjectsReady().Take(1)).WhenCompleted()
                    //     .SelectMany(_ => application.AutoCreate(data, ts));
                    return application.AutoCreate(data, ts);
                });
        }

        private static IObservable<Unit> AutoCreate(this XafApplication application,
            (ThreadSafeDataLayer layer, ((XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data) data,
            IEnumerable<((XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types, Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)> ts) 
            => ts.ToObservable().SelectMany(t => {
                var useObjectSpace = application.UseProviderObjectSpace(objectSpace => objectSpace.GetObjectsCount(t.types.typeInfo.Type, null) == 0
                    ? new UnitOfWork(data.layer)
                        .Use(unitOfWork => new XPCollection(unitOfWork, t.types.classInfo).Cast<object>()
                            .ToNowObservable()
                            .Do(storedObject => objectSpace.EnsureObjectByKey(t.types.typeInfo.Type,
                                unitOfWork.GetKeyValue(storedObject)))
                            .FinallySafe(() => {
                                objectSpace.CommitChanges();
                                objectSpace.CommitChanges();
                            })).ToUnit()
                    : Observable.Empty<Unit>(),
                    t.types.typeInfo.Type);
                return useObjectSpace;
            });


        private static IObservable<Unit> SaveData(this UnitOfWork unitOfWork, object[] modifiedObjects,
            ((IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (
                XPClassInfo classInfo, ITypeInfo typeInfo, bool autoCreate) types) t)
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

                            if (members.xpCustomMemberInfos.Length == 1) {
                                members.xpCustomMemberInfos.First().SetValue(storedToDiskObject,theValue);
                            }
                            else {
                                var value = members.memberInfo.GetValue(modifiedObject);
                                members.xpCustomMemberInfos.ForEach(info => {
                                    var memberInfo = members.memberInfo.MemberTypeInfo.FindMember(info.Name);
                                    var o = memberInfo.GetValue(value);
                                    if (memberInfo.MemberTypeInfo.IsPersistent) {
                                        o = memberInfo.MemberTypeInfo.KeyMember.GetValue(o);
                                    }
                                    info.SetValue(storedToDiskObject, o);
                                });
                            }
                            

                            return Unit.Default;
                        });
                })
                .DoWhen((i, _) => i%100==0,(_, _) => unitOfWork.CommitChanges())
                .FinallySafe(() => {
                    unitOfWork.CommitChanges();
                    unitOfWork.Dispose();
                })
                .ToUnit();

        private static string[] _types = ["LaunchPadProject"];
        // private static string[] _types = ["Network","ServiceNetwork","Asset","ServiceAsset","LaunchPadProject"];
        private static
            IObservable<(ThreadSafeDataLayer layer, ((XPClassInfo classInfo, ITypeInfo typeInfo,bool autoCreate) types,
                Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data)> StoreToDiskData(this XafApplication application) {
            var reflectionDictionary = new ReflectionDictionary();
            return application.TypesInfo.PersistentTypes
                .Select(info => info).Attributed<StoreToDiskAttribute>().ToNowObservable()
                .CheckStoreToDiskAttributeDeclaration()
                .Select(t => {
                    var classInfo = reflectionDictionary.CreateClass(t.typeInfo.Type.Name,new DeferredDeletionAttribute(false),new OptimisticLockingAttribute(OptimisticLockingBehavior.NoLocking));
                    var memberInfos = t.MemberInfos( classInfo);
                    var keyMember = t.typeInfo.FindMember(t.attribute.Key??t.typeInfo.KeyMember.Name);
                    if (keyMember.IsAutoGenerate) throw new Exception($"Auto generate key member {t.typeInfo}");
                    var storeToDiskKeyMember = classInfo.CreateMember(keyMember.Name, keyMember.MemberType, keyMember.Attributes.Where(attribute => attribute.GetType()!=typeof(KeyAttribute))
                            .AddItem(new KeyAttribute()).ToArray());
                    return ((types:classInfo,t.typeInfo,t.attribute.AutoCreate),memberInfos, key: (keyMember, storeToDiskKeyMember), t.attribute.Criteria);

                }).BufferUntilCompleted()
                .CheckIfCanCreateDependencies()
                .SelectMany(data => reflectionDictionary.DataLayer()
                    .Select(layer => (layer, data)))
                .ConcatIgnored(ValidateSchema);
        }

        private static IObservable<Unit> ValidateSchema(
            (ThreadSafeDataLayer layer, ((XPClassInfo types, ITypeInfo typeInfo, bool AutoCreate), Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[] data) data){
            var dataStoreSchemaExplorer = ((IDataStoreSchemaExplorer)XpoDefault.GetConnectionProvider(ConnectionString, AutoCreateOption.None));
            return data.data.Select(t => {
                    var dbTable = dataStoreSchemaExplorer.GetStorageTables(t.Item1.types.TableName).First();
                    return t.memberInfos.Values.Where(t1 => {
                        var infos = t1.xpCustomMemberInfos;
                        return infos.All(info => {
                            var dbColumn = dbTable.Columns.FirstOrDefault(column => column.Name == info.Name);
                            if (dbColumn==null) return false;
                            var columnType = Type.GetType($"System.{dbColumn.ColumnType}");
                            var memberType = info.MemberType.IsEnum ? typeof(int) : info.MemberType.RealType();
                            if (new[]{typeof(TimeSpan)}.Contains(memberType)) {
                                memberType = typeof(long);
                            }
                            return columnType != memberType;
                        });
                        
                    }).ToArray();
                }).ToNowObservable().WhenNotEmpty().BufferUntilCompleted().WhenNotEmpty()
                .SelectMany(t => {
                    var memberInfos = t.SelectMany().Select(t1 => t1.memberInfo).ToArray();
                    return new SchemaCorrectionNeededException(new Exception($"{memberInfos.Select(info => info.Owner.Name).Distinct().JoinComma().Concat(memberInfos.JoinNewLine())}")).Throw<Unit>();
                });
        }

        private static Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> MemberInfos(this (StoreToDiskAttribute attribute, ITypeInfo typeInfo) t, XPClassInfo classInfo){
            var properties = (!t.attribute.Map ? t.attribute.Properties
                : t.typeInfo.Members.Where(info => info.IsPersistent && !info.IsService && !info.IsKey)
                    .Select(info => info.Name)).Distinct();
            return properties.Select(property => t.typeInfo.FindMember(property))
                .Select(memberInfo => (memberInfo, xpCustomMemberInfos: classInfo.CreateMembers( memberInfo)))
                .ToDictionary(t1 => t1.memberInfo.Name, t1 => t1);
        }

        private static XPCustomMemberInfo[] CreateMembers(this XPClassInfo classInfo, IMemberInfo memberInfo) {
            var attribute = memberInfo.FindAttribute<StoreToDiskPropertyAttribute>();
            if (attribute != null)
                return attribute.Properties.Select(prop => memberInfo.MemberTypeInfo.FindMember(prop))
                    .Select(classInfo.CreateMember).ToArray();
            return classInfo.CreateMember(memberInfo).YieldItem().ToArray();
        }

        private static XPCustomMemberInfo CreateMember(this XPClassInfo classInfo, IMemberInfo info) 
            => classInfo.CreateMember(info.Name.Replace(".", "_"), info.MemberTypeInfo.IsPersistent ? info.MemberTypeInfo.KeyMember.MemberType : (
                    info.MemberType==typeof(TimeSpan)?typeof(long):info.MemberType),
                info.Attributes.Where(attribute => attribute is not IndexedAttribute).ToArray());

        private static IObservable<((XPClassInfo types, ITypeInfo typeInfo, bool AutoCreate), Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (
                IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[]> CheckIfCanCreateDependencies(
            this IObservable<((XPClassInfo types, ITypeInfo typeInfo, bool AutoCreate), Dictionary<string, (IMemberInfo memberInfo, XPCustomMemberInfo[] xpCustomMemberInfos)> memberInfos, (
                    IMemberInfo keyMember, XPCustomMemberInfo storeToDiskKeyMember) key, string Criteria)[]> source)
            => source.SelectMany().SelectMany(t => {
                var memberInfos = t.memberInfos.Where(info => {
                        var memberTypeInfo = info.Value.memberInfo.MemberTypeInfo;
                        return memberTypeInfo.IsPersistent && (memberTypeInfo.KeyMember.IsAutoGenerate ||
                            memberTypeInfo.KeyMember.MemberType == typeof(Guid)) && memberTypeInfo
                            .FindAttributes<StoreToDiskPropertyAttribute>().Any();
                    }).ToArray();
                return !memberInfos.Any() ? t.Observe() : new InvalidOperationException(
                            $"Use {nameof(StoreToDiskPropertyAttribute)} on {memberInfos.Select(p => p.Value.memberInfo).JoinNewLine()}").Throw<Unit>().To(t);
            }).BufferUntilCompleted();

        private static IObservable<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)> CheckStoreToDiskAttributeDeclaration(this IObservable<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)> source) 
            => source.If(t => t.typeInfo.IsAbstract, t => new InvalidOperationException($"{nameof(StoreToDiskAttribute)} found on abstract {t.typeInfo.FullName}")
                    .Throw<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)>(), t => t.Observe())
                .If(t => !t.typeInfo.IsPersistent, t => new InvalidOperationException($"{nameof(StoreToDiskAttribute)} found on NonPersistent {t.typeInfo.FullName}")
                    .Throw<(StoreToDiskAttribute attribute, ITypeInfo typeInfo)>(), t => t.Observe());

        private static IObservable<ThreadSafeDataLayer> DataLayer(this ReflectionDictionary reflectionDictionary)
            => AppDomain.CurrentDomain.ExecuteOnce().Select(_ => {
                    var connectionString = ConnectionString;
                    reflectionDictionary.GetClassInfo(typeof(XPObjectType));
                    reflectionDictionary.UpdateSchema( connectionString);
                    var cachedDataStoreProvider = new CachedDataStoreProvider(connectionString);
                    return new ThreadSafeDataLayer(reflectionDictionary, cachedDataStoreProvider
                        .CreateWorkingStore(out var _));
                });

        private static string ConnectionString => ConfigurationManager.ConnectionStrings["StoreToDisk"].ConnectionString;
    }
}