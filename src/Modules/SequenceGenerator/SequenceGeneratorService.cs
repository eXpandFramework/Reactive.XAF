using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.DC.Xpo;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SequenceGenerator{
    public static class SequenceGeneratorService{
        public const int ParallelTransactionExceptionHResult = -2146233079;
        [PublicAPI]
        public const string ParallelTransactionExceptionMessage = "SqlConnection does not support parallel transactions.";
        
        static readonly Subject<Exception> ExceptionsSubject=new Subject<Exception>();
        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        internal static IObservable<TSource> TraceSequenceGeneratorModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, SequenceGeneratorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        static SequenceGeneratorService() => ExceptionsSubject.Do(exception => Tracing.Tracer.LogError(exception)).Subscribe();

        [PublicAPI]
        public static IObservable<Exception> Exceptions => ExceptionsSubject.AsObservable();

        [DebuggerStepThrough][PublicAPI]
        public static void SetSequence(this IObjectSpace objectSpace, Type sequenceType, string sequenceMember,
            Type customSequence = null, long firstSequence = 0, Type sequenceStorageType = null) 
            => objectSpace.SetSequence(sequenceType, sequenceMember, customSequence?.FullName, firstSequence,
                sequenceStorageType);

        [DebuggerStepThrough]
        public static void SetSequence(this IObjectSpace objectSpace, Type sequenceType, string sequenceMember,
            string customSequence = null, long firstSequence = 0, Type sequenceStorageType = null) 
            => SetSequence(sequenceStorageType, sequenceType, objectSpace.UnitOfWork(), sequenceMember, customSequence, firstSequence);

        [DebuggerStepThrough][PublicAPI]
        public static void SetSequence<T>(this IObjectSpace objectSpace, Expression<Func<T, long>> sequenceMember,
            Type customSequence = null, long firstSequence = 0, Type sequenceStorageType = null)
            where T : class, IXPSimpleObject => objectSpace.SetSequence(sequenceMember, firstSequence,
            customSequence?.FullName, sequenceStorageType);

        public static void SetSequence<T>(this IObjectSpace objectSpace, Expression<Func<T, long>> sequenceMember,
            long firstSequence = 0, string customSequence = null, Type sequenceStorageType = null) where T : class, IXPSimpleObject 
            => objectSpace.UnitOfWork().SetSequence(typeof(T), ((MemberExpression) sequenceMember.Body).Member.Name,customSequence,firstSequence,sequenceStorageType);

        public static IObservable<SequenceStorage> Configure(this SequenceStorage sequenceStorage){
            try{
                SetSequence(sequenceStorage.GetType(),sequenceStorage.Type.Type,sequenceStorage.ObjectSpace.UnitOfWork(),sequenceStorage.Member?.Name,sequenceStorage.CustomSequence,sequenceStorage.NextSequence,sequenceStorage);
                return sequenceStorage.ReturnObservable();
            }
            catch (Exception e){
                return Observable.Throw<SequenceStorage>(e);
            }
        }

        static void SetSequence(Type sequenceStorageType,Type sequenceType,UnitOfWork unitOfWork, string sequenceMember, string customSequence = null,long firstSequence=0,ISequenceStorage sequenceStorage=null) {
            sequenceStorageType ??= typeof(SequenceStorage);
            Guard.TypeArgumentIs(typeof(ISequenceStorage),sequenceStorageType,nameof(sequenceStorageType));
            var sequenceTypeName = SequenceStorageKeyNameAttribute.FindConsumer(sequenceType);
            Guard.TypeArgumentIs(typeof(IXPObject),sequenceTypeName,nameof(sequenceType));
            Type customSequenceType = null;
            if (customSequence != null){
                customSequenceType = XafTypesInfo.Instance.FindTypeInfo(customSequence).Type;
                Guard.TypeArgumentIs(typeof(IXPObject),customSequenceType,nameof(customSequence));
            }
            ValidateSequenceParameters(sequenceStorageType, sequenceTypeName,unitOfWork, sequenceMember, customSequenceType);
            sequenceStorage ??= unitOfWork.GetSequenceStorage(sequenceTypeName,false,sequenceStorageType) ?? (ISequenceStorage)sequenceStorageType.CreateInstance(unitOfWork);
            sequenceStorage.Name = sequenceTypeName.GetSequenceName();
            if (firstSequence>sequenceStorage.NextSequence){
                sequenceStorage.NextSequence = firstSequence;
            }

            var customSequenceName = customSequenceType?.GetSequenceName();
            if (customSequenceName!=null){
                sequenceStorage.CustomSequence = customSequence;
            }
            sequenceStorage.SequenceMember=sequenceMember;
            unitOfWork.Save(sequenceStorage);
            unitOfWork.CommitChanges();
        }


        private static void ValidateSequenceParameters(Type storageType,Type sequenceType,UnitOfWork unitOfWork, string sequenceMember, Type customSequenceType) {
            void ValidateSequenceMember(Type type){
                var memberInfo = unitOfWork.Dictionary.QueryClassInfo(type).FindMember(sequenceMember);
                if (memberInfo == null){
                    throw new MemberNotFoundException(type, sequenceMember);
                }

                if (memberInfo.MemberType != typeof(long)){
                    throw new InvalidCastException($"{sequenceMember} Type must be long");
                }
            }

            if (customSequenceType != null){
                var customStorage = unitOfWork.GetSequenceStorage(customSequenceType,sequenceStorageType:storageType);
                if (customStorage == null){
                    throw new InvalidOperationException($"{customSequenceType.FullName} is not found ");
                }

                ValidateSequenceMember(customSequenceType);
            }
            ValidateSequenceMember(sequenceType);
            if ($"{sequenceType.FullName}".Length > 255&&sequenceType.ToTypeInfo().FindAttribute<SequenceStorageKeyNameAttribute>()==null){
                throw new NotSupportedException($"{sequenceType.Name} FullName Length is greater than 255. Use the {nameof(SequenceStorageKeyNameAttribute)}");
            }
        }

        [DebuggerStepThrough][PublicAPI]
        public static void SetSequence<T>(this UnitOfWork unitOfWork, Expression<Func<T, long>> sequenceMember,
            string customSequence = null, long firstSequence = 0, Type sequenceStorageType = null)
            where T : class, IXPObject => unitOfWork.SetSequence(typeof(T),
            ((MemberExpression) sequenceMember.Body).Member.Name, customSequence, firstSequence, sequenceStorageType);

        [DebuggerStepThrough]
        public static void SetSequence(this UnitOfWork unitOfWork, Type sequenceType, string sequenceMember,
            string customSequence = null, long firstSequence = 0, Type sequenceStorageType = null) 
            => SetSequence(sequenceStorageType, sequenceType, unitOfWork, sequenceMember, customSequence, firstSequence);

        [DebuggerStepThrough][PublicAPI]
        public static ISequenceStorage GetSequenceStorage(this IObjectSpace objectSpace, Type objectType,
            bool customSequenceLookup = true, Type sequenceStorageType = null) 
            => objectSpace.UnitOfWork().GetSequenceStorage(objectType, customSequenceLookup, sequenceStorageType);

        public static ISequenceStorage GetSequenceStorage(this UnitOfWork unitOfWork,Type objectType,bool customSequenceLookup=true,Type sequenceStorageType=null){
            sequenceStorageType ??= typeof(SequenceStorage);
            Guard.TypeArgumentIs(typeof(ISequenceStorage),sequenceStorageType,nameof(sequenceStorageType));
            Guard.TypeArgumentIs(typeof(IXPObject),objectType,nameof(objectType));
            var sequenceStorage = (ISequenceStorage) unitOfWork.GetObjectByKey(sequenceStorageType,objectType.GetSequenceName(),true);
            return customSequenceLookup&&sequenceStorage?.CustomSequence != null ? (ISequenceStorage) unitOfWork.GetObjectByKey(sequenceStorageType,
                XafTypesInfo.Instance.FindTypeInfo(sequenceStorage.CustomSequence).Type.GetSequenceName(), true) : sequenceStorage;
        }

        public static string GetSequenceName(this Type objectType){
            Guard.TypeArgumentIs(typeof(IXPObject),objectType,nameof(objectType));
            var attribute = objectType.ToTypeInfo().FindAttribute<SequenceStorageKeyNameAttribute>();
            return attribute != null ? attribute.Type.FullName : objectType.FullName;
        }

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager,Type sequenceStorageType=null){
            sequenceStorageType ??= typeof(SequenceStorage);
            Guard.TypeArgumentIs(typeof(ISequenceStorage),sequenceStorageType,nameof(sequenceStorageType));
            return manager.WhenApplication(application => application.WhenCompatibilityChecked().FirstAsync().Select(xafApplication => xafApplication.ObjectSpaceProvider)
                .SelectMany(provider => provider.SequenceGeneratorDatalayer()
                    .SelectMany(dataLayer => application.WhenObjectSpaceCreated().GenerateSequences(dataLayer,sequenceStorageType)
                        .Merge(application.Security.AddAnonymousType(sequenceStorageType).ToObservable())))
                .Merge(application.ConfigureDetailViewSequenceStorage()).ToUnit());
        }

        private static IObservable<object> ConfigureDetailViewSequenceStorage(this XafApplication application) 
            => application.WhenViewCreated()
                .Where(_ => _ is ObjectView objectView && typeof(ISequenceStorage).IsAssignableFrom(objectView.ObjectTypeInfo.Type))
                .Select(_ => _).Cast<ObjectView>()
                .SelectMany(view => view.ObjectSpace.WhenCommiting()
                    .SelectMany(_ => Configure(application, view, _)))

                .Select(unit => new object()).IgnoreElements();

        private static IObservable<object> Configure(XafApplication application, ObjectView view, (IObjectSpace objectSpace, CancelEventArgs e) _) 
            => view.ObjectSpace.ModifiedObjects.Cast<SequenceStorage>().Where(storage => !storage.ObjectSpace.IsObjectToDelete(storage))
                .ToObservable()
                .SelectMany(storage => storage.Configure().HandleErrors(application, _.e)).ToUnit()
                .To(new object());

        private static readonly ISubject<object> SequenceSubject = Subject.Synchronize(new Subject<object>());

        [PublicAPI]
        public static IObservable<object> Sequence => SequenceSubject.AsObservable();

        private static IObservable<object> GenerateSequences(this IObservable<IObjectSpace> source, IDataLayer dataLayer,Type sequenceStorageType=null) 
            => source.SelectMany(space => Observable.Defer(() => space.GetObjectForSave(dataLayer).SelectMany(e => e.GenerateSequences(sequenceStorageType))
                        .TakeUntil(space.WhenCommited().Select(objectSpace => objectSpace)))
                    .RepeatWhen(observable => observable.Where(o => !space.IsDisposed).Do(o => {}))
                )
                .Select(o => o)
                .Do(SequenceSubject)
                .TraceSequenceGeneratorModule();

        static IObservable<(ObjectManipulationEventArgs e, ExplicitUnitOfWork explicitUnitOfWork)> GetObjectForSave(this IObjectSpace objectSpace, IDataLayer dataLayer ){
            var unitOfWork = objectSpace.UnitOfWork();
            var unitOfWorks = new ConcurrentDictionary<IObjectSpace,ExplicitUnitOfWork>();
            return objectSpace.WhenCommiting().SelectMany(_ => {
                if (!unitOfWorks.TryGetValue(objectSpace, out var explicitUnitOfWork)) {
                    explicitUnitOfWork=new ExplicitUnitOfWork(dataLayer);
                    unitOfWorks.TryAdd(objectSpace, explicitUnitOfWork);
                    return unitOfWork.WhenObjectSaving().Distinct(e => e.Object)
                        .Where(e => e.Session.IsNewObject(e.Object))
                        .Select(e => (e,explicitUnitOfWork))
                        .TakeUntil(objectSpace.WhenCommited().ToUnit().Merge(objectSpace.WhenRollingBack().Select(tuple => tuple).ToUnit()))
                        .Finally(() => {
                            explicitUnitOfWork.Close();
                            unitOfWorks.TryRemove(objectSpace, out explicitUnitOfWork);
                        });
                }
                return Observable.Empty<(ObjectManipulationEventArgs e, ExplicitUnitOfWork explicitUnitOfWork)>();
            });
        }

        private static IObservable<object> GenerateSequences(this (ObjectManipulationEventArgs e,ExplicitUnitOfWork explicitUnitOfWork) t, Type sequenceStorageType) 
            => Observable.Defer(() => {
                    var explicitUnitOfWork = t.explicitUnitOfWork;
                    var afterCommit = t.e.Session.WhenAfterCommitTransaction().FirstAsync().Select(pattern => {
                            explicitUnitOfWork.CommitChanges();
                            explicitUnitOfWork.Close();
                            return default(object);
                        })
                        .DisposeOnException(explicitUnitOfWork)
                        .IgnoreElements();
                    var currentThreadScheduler = Scheduler.CurrentThread;
                    var whenTransactionFailed = t.e.Session.WhenTransactionFailed( currentThreadScheduler, explicitUnitOfWork);
                    return afterCommit.Merge(explicitUnitOfWork.GenerateSequences(t.e.Object,sequenceStorageType), currentThreadScheduler)
                        .Merge(whenTransactionFailed,currentThreadScheduler);
                })
                .RetryWhen(exceptions => exceptions.Select(exception => exception).RetryException().Do(ExceptionsSubject.OnNext));

        
        static readonly object Locker=new object();
        private static IObservable<object> GenerateSequences(this ExplicitUnitOfWork explicitUnitOfWork,object theObject,Type sequenceStorageType){
            var classInfoProvider = ((IXPClassInfoProvider) theObject);
            lock (Locker){
                var sequenceStorage = explicitUnitOfWork.GetSequenceStorage(theObject.GetType(),sequenceStorageType:sequenceStorageType);
                if (sequenceStorage!=null){
                    var memberInfo = classInfoProvider.ClassInfo.GetMember(sequenceStorage.SequenceMember);
                    memberInfo.SetValue(theObject, sequenceStorage.NextSequence);
                    sequenceStorage.NextSequence++;
                    try{
                        explicitUnitOfWork.FlushChanges();
                    }
                    catch (Exception){
                        sequenceStorage.NextSequence--;
                        throw;
                    }
                    return theObject.ReturnObservable();
                }
                return Observable.Empty<object>();
            }
        }

        private static IObservable<EventPattern<EventArgs>> WhenTransactionFailed(this Session session,
            IScheduler scheduler, ExplicitUnitOfWork explicitUnitOfWork) 
            => session.WhenAfterRollbackTransaction().Select(pattern => pattern)
                .Merge(session.WhenFailedCommitTransaction().Select(pattern => pattern),scheduler)
                .IgnoreElements()
                .DisposeOnException(explicitUnitOfWork)
                .Do(_ => explicitUnitOfWork.Close()).FirstAsync();

        private static IObservable<Exception> RetryException(this IObservable<Exception> source) 
            => source.OfType<Exception>().Where(exception => exception.HResult == ParallelTransactionExceptionHResult)
                .TraceSequenceGeneratorModule(exception => $"{exception.GetType().Name}, {exception.Message}");

        public static void Close(this ExplicitUnitOfWork explicitUnitOfWork){
            explicitUnitOfWork.Disconnect();
            explicitUnitOfWork.Dispose();
        }

        private static IObservable<T> DisposeOnException<T>(this IObservable<T> source,ExplicitUnitOfWork explicitUnitOfWork) 
            => source.Catch<T, Exception>(exception => {
                explicitUnitOfWork.Close();
                return Observable.Throw<T>(exception);
            });


        public static IObservable<IDataLayer> SequenceGeneratorDatalayer(this  IObjectSpaceProvider objectSpaceProvider) 
            => Observable.Defer(() => XpoDefault.GetDataLayer(objectSpaceProvider.GetConnectionString(),
                ((TypesInfo) objectSpaceProvider.TypesInfo).EntityStores.OfType<XpoTypeInfoSource>().First().XPDictionary, AutoCreateOption.None
            ).ReturnObservable()).SubscribeReplay()
                .TraceSequenceGeneratorModule();
    }
}