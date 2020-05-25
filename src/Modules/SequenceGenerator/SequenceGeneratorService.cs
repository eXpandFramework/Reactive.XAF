using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using Xpand.Extensions.XAF.Security;
using Xpand.Extensions.XAF.TypesInfo;
using Xpand.Extensions.XAF.Xpo;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SequenceGenerator{
    public static class SequenceGeneratorService{
        public const int ParallelTransactionExceptionHResult = -2146233079;
        [PublicAPI]
        public const string ParallelTransactionExceptionMessage = "SqlConnection does not support parallel transactions.";
        
        static readonly Subject<Exception> ExceptionsSubject=new Subject<Exception>();
        internal static IObservable<TSource> TraceSequenceGeneratorModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, SequenceGeneratorModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        static SequenceGeneratorService() => ExceptionsSubject.Do(exception => Tracing.Tracer.LogError(exception)).Subscribe();

        [PublicAPI]
        public static IObservable<Exception> Exceptions => ExceptionsSubject.AsObservable();

        [DebuggerStepThrough][PublicAPI]
        public static void SetSequence(this IObjectSpace objectSpace, Type sequenceType, string sequenceMember,
            Type customSequence = null, long firstSequence = 0, Type sequenceStorageType = null) =>
            objectSpace.SetSequence(sequenceType, sequenceMember, customSequence?.FullName, firstSequence,
                sequenceStorageType);

        [DebuggerStepThrough]
        public static void SetSequence(this IObjectSpace objectSpace, Type sequenceType, string sequenceMember,
            string customSequence = null, long firstSequence = 0, Type sequenceStorageType = null) => SetSequence(
            sequenceStorageType, sequenceType, objectSpace.UnitOfWork(), sequenceMember, customSequence, firstSequence);

        [DebuggerStepThrough][PublicAPI]
        public static void SetSequence<T>(this IObjectSpace objectSpace, Expression<Func<T, long>> sequenceMember,
            Type customSequence = null, long firstSequence = 0, Type sequenceStorageType = null)
            where T : class, IXPSimpleObject => objectSpace.SetSequence(sequenceMember, firstSequence,
            customSequence?.FullName, sequenceStorageType);

        public static void SetSequence<T>(this IObjectSpace objectSpace, Expression<Func<T, long>> sequenceMember,
            long firstSequence = 0, string customSequence = null, Type sequenceStorageType = null) where T : class, IXPSimpleObject =>
            objectSpace.UnitOfWork().SetSequence(typeof(T), ((MemberExpression) sequenceMember.Body).Member.Name,customSequence,firstSequence,sequenceStorageType);

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
            var sequencTypeName = SequenceStorageKeyNameAttribute.FindConsumer(sequenceType);
            Guard.TypeArgumentIs(typeof(IXPObject),sequencTypeName,nameof(sequenceType));
            Type customSequenceType = null;
            if (customSequence != null){
                customSequenceType = XafTypesInfo.Instance.FindTypeInfo(customSequence).Type;
                Guard.TypeArgumentIs(typeof(IXPObject),customSequenceType,nameof(customSequence));
            }
            ValidateSequenceParameters(sequenceStorageType, sequencTypeName,unitOfWork, sequenceMember, customSequenceType);
            sequenceStorage ??= unitOfWork.GetSequenceStorage(sequencTypeName,false,sequenceStorageType) ?? (ISequenceStorage)sequenceStorageType.CreateInstance(unitOfWork);
            sequenceStorage.Name = sequencTypeName.GetSequenceName();
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


        private static void ValidateSequenceParameters(Type sequqneceStorageType,Type sequenceType,UnitOfWork unitOfWork, string sequenceMember, Type customSequenceType) {
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
                var customStorage = unitOfWork.GetSequenceStorage(customSequenceType,sequenceStorageType:sequqneceStorageType);
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
            string customSequence = null, long firstSequence = 0, Type sequenceStorageType = null) => SetSequence(
            sequenceStorageType, sequenceType, unitOfWork, sequenceMember, customSequence, firstSequence);

        [DebuggerStepThrough][PublicAPI]
        public static ISequenceStorage GetSequenceStorage(this IObjectSpace objectSpace, Type objectType,
            bool customSequenceLookup = true, Type sequenceStorageType = null) => objectSpace.UnitOfWork()
            .GetSequenceStorage(objectType, customSequenceLookup, sequenceStorageType);

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

        internal static IObservable<object> Connect(this XafApplication application,Type sequenceStorageType=null){
            sequenceStorageType ??= typeof(SequenceStorage);
            Guard.TypeArgumentIs(typeof(ISequenceStorage),sequenceStorageType,nameof(sequenceStorageType));
            var objectSpaceProvider = application.ObjectSpaceProvider == null ? application.WhenSetupComplete()
                .Select(_ => application.ObjectSpaceProvider) : application.ObjectSpaceProvider.ReturnObservable();

            return objectSpaceProvider.SelectMany(provider => provider.SequenceGeneratorDatalayer()
                    .SelectMany(dataLayer => application.WhenObjectSpaceCreated().GenerateSequences(dataLayer,sequenceStorageType)
                        .Merge(application.Security.AddAnonymousType(sequenceStorageType).ToObservable())))
                    .Merge(application.ConfigureDetailViewSequenceStorage());
        }

        private static IObservable<object> ConfigureDetailViewSequenceStorage(this XafApplication application) =>
            application.WhenViewCreated()
                .Where(_ => _ is ObjectView objectView && typeof(ISequenceStorage).IsAssignableFrom(objectView.ObjectTypeInfo.Type))
                .Select(_ => _).Cast<ObjectView>()
                .SelectMany(view => view.ObjectSpace.WhenCommiting()
                    .SelectMany(_ => Configure(application, view, _)))

                .Select(unit => new object()).IgnoreElements();

        private static IObservable<object> Configure(XafApplication application, ObjectView view, (IObjectSpace objectSpace, CancelEventArgs e) _) =>
            view.ObjectSpace.ModifiedObjects.Cast<SequenceStorage>().Where(storage => !storage.ObjectSpace.IsObjectToDelete(storage))
                .ToObservable()
                .SelectMany(storage => storage.Configure().HandleErrors(application, _.e)).ToUnit()
                .To(new object());

        private static readonly ISubject<object> SequenceSubject = Subject.Synchronize(new Subject<object>());

        [PublicAPI]
        public static IObservable<object> Sequence => SequenceSubject.AsObservable();

        private static IObservable<object> GenerateSequences(this IObservable<IObjectSpace> source, IDataLayer dataLayer,Type sequenceStorageType=null) =>
            source.SelectMany(space => Observable.Defer(() => space.UnitOfWork().WhenObjectSaving().Select(session => session).Distinct()
                    .TakeUntil(space.UnitOfWork().WhenAfterCommitTransaction())).RepeatWhen(observable => observable.Where(o => space.UnitOfWork() != null)))
                .SelectMany(session => session.GenerateSequences(dataLayer, sequenceStorageType))
                .Do(SequenceSubject)
                .TraceSequenceGeneratorModule();

        private static IObservable<object> GenerateSequences(this Session session,IDataLayer dataLayer, Type sequenceStorageType) =>
            Observable.Defer(() => {
                    var explicitUnitOfWork = new ExplicitUnitOfWork(dataLayer);
                    var generateSequences = session.GenerateSequences(explicitUnitOfWork,sequenceStorageType).Publish().RefCount();
                    var afterCommited = session.WhenAfterCommitTransaction().FirstAsync().SelectMany(pattern => {
                            explicitUnitOfWork.CommitChanges();
                            explicitUnitOfWork.Close();
                            return generateSequences;
                        })
                        .DisposeOnException(explicitUnitOfWork)
                        .IgnoreElements();
                    var currentThreadScheduler = Scheduler.CurrentThread;
                    var whenTransacationFailed = session.WhenTransacationFailed( currentThreadScheduler, explicitUnitOfWork);
                    return afterCommited.Merge(generateSequences, currentThreadScheduler)
                        .Merge(whenTransacationFailed,currentThreadScheduler);
                })
                .RetryWhen(exceptions => exceptions.RetryException().Do(ExceptionsSubject.OnNext));

        private static IObservable<EventPattern<EventArgs>> WhenTransacationFailed(this Session session,
            IScheduler scheduler, ExplicitUnitOfWork explicitUnitOfWork) =>
            session.WhenAfterRollbackTransaction()
                .Merge(session.WhenFailedCommitTransaction(),scheduler)
                .IgnoreElements()
                .DisposeOnException(explicitUnitOfWork)
                .Do(_ => explicitUnitOfWork.Close()).FirstAsync();

        private static IObservable<Exception> RetryException(this IObservable<Exception> source) =>
            source.OfType<Exception>().Where(exception => exception.HResult == ParallelTransactionExceptionHResult)
                .TraceSequenceGeneratorModule(exception => $"{exception.GetType().Name}, {exception.Message}");

        private static IObservable<object> GenerateSequences(this Session session,  ExplicitUnitOfWork explicitUnitOfWork,Type sequenceStorageType) =>
            session.GetObjectsToSave().Cast<object>()
                .GroupBy(o => o.GetType())
                .SelectMany(objects => {
                    var sequenceStorage = explicitUnitOfWork.GetSequenceStorage(objects.Key,sequenceStorageType:sequenceStorageType);
                    return sequenceStorage != null ? objects.Where(o => ((ISessionProvider) o).Session.IsNewObject(o))
                        .Cast<IXPClassInfoProvider>().GenerateNextSequences(explicitUnitOfWork, sequenceStorage) : Enumerable.Empty<object>();
                })
                .ToObservable()
                .DisposeOnException(explicitUnitOfWork);

        public static void Close(this ExplicitUnitOfWork explicitUnitOfWork){
            explicitUnitOfWork.Disconnect();
            explicitUnitOfWork.Dispose();
        }
        private static IObservable<T> DisposeOnException<T>(this IObservable<T> source,ExplicitUnitOfWork explicitUnitOfWork) =>
            source.Catch<T, Exception>(exception => {
                explicitUnitOfWork.Close();
                return Observable.Throw<T>(exception);
            });

        private static IEnumerable<IXPClassInfoProvider> GenerateNextSequences(this IEnumerable<IXPClassInfoProvider> source,ExplicitUnitOfWork explicitUnitOfWork, ISequenceStorage sequenceStorage) =>
            source.Select(o => {
                var memberInfo = o.ClassInfo.GetMember(sequenceStorage.SequenceMember);
                memberInfo.SetValue(o, sequenceStorage.NextSequence);
                sequenceStorage.NextSequence++;
                explicitUnitOfWork.FlushChanges();
                return o;
            });

        public static IObservable<IDataLayer> SequenceGeneratorDatalayer(this  IObjectSpaceProvider objectSpaceProvider) =>
            Observable.Defer(() => XpoDefault.GetDataLayer(objectSpaceProvider.GetConnectionString(),
                ((TypesInfo) objectSpaceProvider.TypesInfo).EntityStores.OfType<XpoTypeInfoSource>().First().XPDictionary, AutoCreateOption.None
            ).ReturnObservable()).SubscribeReplay();
    }
}