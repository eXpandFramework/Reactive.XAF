using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Shouldly;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests{
    public abstract class SequenceGeneratorTestsCommonTests:BaseTest{
        static SequenceGeneratorTestsCommonTests() {
            var path = $"{AppDomain.CurrentDomain.ApplicationPath()}\\microsoft.data.sqlclient.dll";
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
        protected  SequenceGeneratorModule SequenceGeneratorModule( XafApplication application=null,Platform platform=Platform.Win){
            application ??= NewApplication(platform);
            var sequenceGeneratorModule = application.AddModule<SequenceGeneratorModule>(typeof(TestObject).Assembly.GetTypes().Where(type => typeof(IXPSimpleObject).IsAssignableFrom(type)).Concat(new []{typeof(CustomSequenceTypeName)}).ToArray());
            // application.Model.ToReactiveModule<IModelReactiveModuleLogger>().ReactiveLogger.TraceSources.Enabled = false;
            return sequenceGeneratorModule;
        }

        protected XafApplication NewApplication(Platform platform=Platform.Win,bool usePersistentStorage=true,bool handleExceptions=true){
            return platform.NewApplication<SequenceGeneratorModule>(usePersistentStorage:usePersistentStorage,handleExceptions:handleExceptions);
        }

        protected void SetSequences(XafApplication application,Type sequenceStorageType=null){
            sequenceStorageType ??= typeof(SequenceStorage);
            using var objectSpace = application.CreateObjectSpace();
            var emptyType = Type.EmptyTypes.FirstOrDefault();
            objectSpace.SetSequence<TestObject>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<TestType2Object>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<TestObject2>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<TestObject3>(o => o.SequentialNumber,typeof(TestObject2),sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<CustomSquenceNameTestObject>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<Child>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<ParentSequencial>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            objectSpace.SetSequence<ParentSequencialWithChildOnSaving>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
        }
        
        protected void AssertNextSequences<T>(XafApplication application,int itemsCount, TestObserver<T> nextSequenceTest) where T:ISequentialNumber{
            nextSequenceTest.ItemCount.ShouldBe(itemsCount );
            using var space = application.CreateObjectSpace();
            for (int i = 0; i < itemsCount ; i++){
                var item = nextSequenceTest.Items[i];
                item.SequentialNumber.ShouldBe(i);
                space.GetObject(item).SequentialNumber.ShouldBe(i);
            }
        }
        
        protected IDataLayer NewSimpleDataLayer(XafApplication application)
            => XpoDefault.GetDataLayer(
                application.ConnectionString,
                XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary,
                AutoCreateOption.DatabaseAndSchema
            );


        protected IObservable<Unit> TestObjects<T>(XafApplication application,bool parallel, int count = 100, int objectSpaceCount = 1, Action beforeSave = null){
            if (parallel){
                return Observable.Range(1, count).SelectMany(_ => Observable.Defer(() => Observable.Start(() => {
                    for (int j = 0; j < objectSpaceCount; j++) {
                        using var objectSpace = application.CreateObjectSpace();
                        objectSpace.CreateObject<T>();
                        beforeSave?.Invoke();
                        objectSpace.CommitChanges();
                    }
                })));
            }
            return Observable.Start(() => {
                    for (int i = 0; i < objectSpaceCount; i++) {
                        using var objectSpace = application.CreateObjectSpace();
                        for (int j = 0; j < count; j++){
                            objectSpace.CreateObject<T>();
                            beforeSave?.Invoke();
                            objectSpace.CommitChanges();
                        }
                    }
            });
        }

        protected IObservable<Unit> TestObjects(XafApplication application,bool parallel,int count = 100,int objectSpaceCount=1,Action beforeSave=null){
            return TestObjects<TestObject>(application,parallel, count, objectSpaceCount, beforeSave);
        }
    }
}