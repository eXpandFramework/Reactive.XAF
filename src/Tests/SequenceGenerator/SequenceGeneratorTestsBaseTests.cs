using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using JetBrains.Annotations;
using Shouldly;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests{
    public abstract class SequenceGeneratorTestsBaseTests:BaseTest{
        protected  SequenceGeneratorModule SequenceGeneratorModule( XafApplication application=null,Platform platform=Platform.Win){
            application ??= NewApplication(platform);
            return application.AddModule<SequenceGeneratorModule>(typeof(TestObject).Assembly.GetTypes().Where(type => typeof(IXPSimpleObject).IsAssignableFrom(type)).Concat(new []{typeof(CustomSequenceTypeName)}).ToArray());
        }

        protected XafApplication NewApplication(Platform platform=Platform.Win){
            return platform.NewApplication<SequenceGeneratorModule>(usePersistentStorage:true);
        }

        protected void SetSequences(XafApplication application,Type sequenceStorageType=null){
            sequenceStorageType ??= typeof(SequenceStorage);
            using (var objectSpace = application.CreateObjectSpace()){
                var emptyType = Type.EmptyTypes.FirstOrDefault();
                objectSpace.SetSequence<TestObject>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
                objectSpace.SetSequence<TestType2Object>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
                objectSpace.SetSequence<TestObject2>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
                objectSpace.SetSequence<TestObject3>(o => o.SequentialNumber,typeof(TestObject2),sequenceStorageType: sequenceStorageType);
                objectSpace.SetSequence<CustomSquenceNameTestObject>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
                objectSpace.SetSequence<Child>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
                objectSpace.SetSequence<ParentSequencial>(o => o.SequentialNumber,emptyType,sequenceStorageType: sequenceStorageType);
            }
        }
        
        protected void AssertNextSequences<T>(XafApplication application,int itemsCount, TestObserver<T> nextSequenceTest) where T:ISequentialNumber{
            nextSequenceTest.ItemCount.ShouldBe(itemsCount );
            using (var space = application.CreateObjectSpace()){
                for (int i = 0; i < itemsCount ; i++){
                    var item = nextSequenceTest.Items[i];
                    item.SequentialNumber.ShouldBe(i);
                    space.GetObject(item).SequentialNumber.ShouldBe(i);
                }
            }
        }
        
        protected IDataLayer NewSimpleDataLayer(XafApplication application) =>
            XpoDefault.GetDataLayer(
                application.ConnectionString,
                XpoTypesInfoHelper.GetXpoTypeInfoSource().XPDictionary,
                AutoCreateOption.DatabaseAndSchema
            );


        [UsedImplicitly]
        protected IObservable<Unit> DeleteAndCreate(XafApplication application){
            var deleteOneCreateOne = Observable.Defer(() => Observable.Start(() => {
                using (var objectSpace = application.CreateObjectSpace()){
                    var sequenceGeneratorTestObject =
                        objectSpace.GetObjectsQuery<TestObject>().First();
                    sequenceGeneratorTestObject.Title = "delete";
                    objectSpace.Delete(sequenceGeneratorTestObject);
                    objectSpace.CreateObject<TestObject>();
                    objectSpace.CommitChanges();
                }
            }));
            return deleteOneCreateOne;
        }

        protected IObservable<Unit> TestObjects<T>(XafApplication application,bool parallel, int count = 100, int objectSpaceCount = 1, Action beforeSave = null){
            if (!parallel){
                return Observable.Range(1, count).SelectMany(i => Observable.Defer(() => Observable.Start(() => {
                    for (int j = 0; j < objectSpaceCount; j++){
                        using (var objectSpace = application.CreateObjectSpace()){
                            objectSpace.CreateObject<T>();
                            beforeSave?.Invoke();
                            objectSpace.CommitChanges();
                            Debug.WriteLine("");
                        }
                    }
                })));
            }
            return Observable.Start(() => {
                    for (int i = 0; i < objectSpaceCount; i++){
                        using (var objectSpace = application.CreateObjectSpace()){
                            for (int j = 0; j < count; j++){
                                objectSpace.CreateObject<T>();
                                beforeSave?.Invoke();
                                objectSpace.CommitChanges();
                            }
                        }

                    }
            });
        }

        protected IObservable<Unit> TestObjects(XafApplication application,bool parallel,int count = 100,int objectSpaceCount=1,Action beforeSave=null){
            return TestObjects<TestObject>(application,parallel, count, objectSpaceCount, beforeSave);
        }
    }
}