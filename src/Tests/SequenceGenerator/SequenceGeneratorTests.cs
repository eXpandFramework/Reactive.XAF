using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO.BrokeroTests.SequenceGenerator.BO;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Validation.Win;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests{
    [NonParallelizable]
    public class SequenceGeneratorTests:SequenceGeneratorTestsBaseTests{
        
        [Test]
        [XpandTest()]
        public async Task Cache_DataLayer(){
	        using var application = SequenceGeneratorModule().Application;
	        var datalayer = application.ObjectSpaceProvider.SequenceGeneratorDatalayer();

	        (await datalayer).ShouldBe(await datalayer);
        }

        [Test]
        [XpandTest()]
        public async Task Observe_ObjectSpace_Commiting_On_Sequence_Generator_Thread(){
	        using var application = SequenceGeneratorModule().Application;
	        var eventLoopScheduler = new EventLoopScheduler(start => new Thread(start){IsBackground = true});
	        var commiting = application.WhenObjectSpaceCreated()
		        .SelectMany(space => space.WhenCommiting())
		        .Select(_ => Environment.CurrentManagedThreadId);
	        var sequenceGeneratorDatalayer = application.ObjectSpaceProvider.SequenceGeneratorDatalayer()
		        .ObserveOn(eventLoopScheduler)
		        .Select(_ => Environment.CurrentManagedThreadId);
	        var subscribeReplay = commiting.SelectMany(commits => sequenceGeneratorDatalayer.Select(sequenceGenerator => (sequenceGenerator,commits)))
		        .ObserveOn(eventLoopScheduler)
		        .Select(_ => (_.sequenceGenerator,_.commits,Environment.CurrentManagedThreadId))
		        .SubscribeReplay();

	        await TestObjects(application, true, 2);
	        var tuple = await subscribeReplay.FirstAsync();
	        tuple.sequenceGenerator.ShouldBe(tuple.CurrentManagedThreadId);
	        tuple.commits.ShouldNotBe(tuple.CurrentManagedThreadId);
	        eventLoopScheduler.Dispose();
        }


        [Test]
        [XpandTest]
        public async Task Do_Not_Increase_Sequence_When_Updating_Objects(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<TestObject>().Test();
            
	        await TestObjects(application, false, 1);

	        using var objectSpace = application.CreateObjectSpace();
	        var testObject = objectSpace.GetObject(nextSequenceTest.Items.First());
	        objectSpace.GetObjectsCount(typeof(SequenceStorage),null).ShouldBeGreaterThan(0);
	        testObject.Title = Guid.NewGuid().ToString();
	        objectSpace.CommitChanges();
                
	        nextSequenceTest.ItemCount.ShouldBe(1);
	        nextSequenceTest.Items[0].Title.ShouldNotBe(testObject.Title);
        }

        [Combinatorial]
        [XpandTest(LongTimeout)]
        public async Task Increase_Sequence_When_Saving_New_Objects([Values(2, 102, 500)] int itemsCount, [Values(true, false)] bool parallel, [Values(1, 2)] int objectSpaceCount){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<TestObject>().Test();

	        await TestObjects(application, parallel, itemsCount, objectSpaceCount)
		        .Timeout(TimeSpan.FromSeconds(100));
            
	        AssertNextSequences(application,itemsCount*objectSpaceCount, nextSequenceTest);
        }

        [Test]
        [XpandTest]
        public void Increase_Sequence_When_saving_Nested_New_Objects_from_subsequent_commits(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<Child>().Test();

	        using (var objectSpace = application.CreateObjectSpace()){
		        var parent = objectSpace.CreateObject<Parent>();
		        var objectCount = 10;
		        for (int i = 0; i < objectCount; i++){
			        parent.Childs.Add(objectSpace.CreateObject<Child>());    
		        }
		        objectSpace.CommitChanges();
		        AssertNextSequences(application, objectCount,nextSequenceTest);

		        parent = objectSpace.CreateObject<Parent>();
		        for (int i = 0; i < objectCount; i++){
			        parent.Childs.Add(objectSpace.CreateObject<Child>());    
		        }
		        objectSpace.CommitChanges();
                
		        AssertNextSequences(application, objectCount*2,nextSequenceTest);
	        }
	        nextSequenceTest.Dispose();
        }
        [Test][XpandTest]
        public void Increase_Sequence_When_creating_Nested_New_Objects_on_non_sequential_parent_saving(){
            Increase_Sequence_When_creating_Nested_New_Objects_on_parent_saving<ParentNonSequencialWithChildOnSaving>();
        }

        [Test][XpandTest]
        public void Increase_Sequence_When_creating_Nested_New_Objects_on_sequential_parent_saving(){
            Increase_Sequence_When_creating_Nested_New_Objects_on_parent_saving<ParentSequencialWithChildOnSaving>();
        }

        private void Increase_Sequence_When_creating_Nested_New_Objects_on_parent_saving<TParent>() where TParent:Parent{
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<Child>().Test();
                
	        using (var objectSpace = application.CreateObjectSpace()){
		        objectSpace.CreateObject<TParent>();
		        objectSpace.CommitChanges();
		        AssertNextSequences(application, 1, nextSequenceTest);
	        }
	        nextSequenceTest.Dispose();
        }

        [Test][XpandTest]
        public void Increase_Sequence_When_saving_Parent_Child_New_Objects(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextParentSequenceTest = SequenceGeneratorService.Sequence.OfType<ParentSequencial>().Test();
	        var nextChildSequenceTest = SequenceGeneratorService.Sequence.OfType<Child>().Test();

	        using var objectSpace = application.CreateObjectSpace();
	        for (int j = 0; j < 5; j++){
		        var parent = objectSpace.CreateObject<ParentSequencial>();
		        for (int i = 0; i < 5; i++){
			        parent.Childs.Add(objectSpace.CreateObject<Child>());    
		        }
	        }
	        objectSpace.CommitChanges();
                
	        AssertNextSequences(application, 5,nextParentSequenceTest);
	        AssertNextSequences(application, 25,nextChildSequenceTest);
                
                
	        for (int j = 0; j < 5; j++){
		        var parent = objectSpace.CreateObject<ParentSequencial>();
		        for (int i = 0; i < 5; i++){
			        parent.Childs.Add(objectSpace.CreateObject<Child>());    
		        }
	        }
	        objectSpace.CommitChanges();
                
	        AssertNextSequences(application, 10,nextParentSequenceTest);
	        AssertNextSequences(application, 50,nextChildSequenceTest);
        }

        [Test]
        [XpandTest]
        public async Task Increase_Sequence_When_Saving_Different_Type_Objects(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<ISequentialNumber>().SubscribeReplay();

	        await TestObjects<TestType2Object>(application, false, 1)
			        .Concat(TestObjects(application, false,1))
			        .Concat(TestObjects<TestObjectNotRegistered>(application, false, 1))
		        ;
                
	        AssertNextSequences(application, 1,nextSequenceTest.OfType<TestObject>().Test());
	        AssertNextSequences(application, 1,nextSequenceTest.OfType<TestType2Object>().Test());
	        nextSequenceTest.OfType<TestObjectNotRegistered>().Test().ItemCount.ShouldBe(0);
        }
        
        [Test][XpandTest]
        public async Task Increase_Sequence_When_SequenceStorageKeyAttribute(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<CustomSquenceNameTestObject>().Test();

	        await TestObjects<CustomSquenceNameTestObject>(application, false, 1);
            
	        AssertNextSequences(application, 1,nextSequenceTest);
	        using var objectSpace = application.CreateObjectSpace();
	        objectSpace.GetSequenceStorage(typeof(CustomSquenceNameTestObject)).Name.ShouldBe(typeof(CustomSequenceTypeName).FullName);
        }


        [Test]
        [XpandTest]
        public async Task Custom_Sequence_Type(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        var nextSequenceTest = SequenceGeneratorService.Sequence.OfType<ISequentialNumber>().Test();

	        await TestObjects<TestObject2>(application, false, 1).Merge(TestObjects<TestObject3>(application, false,1));

	        using (var objectSpace = application.CreateObjectSpace()){
		        objectSpace.GetSequenceStorage(typeof(TestObject2)).ShouldBe(
			        objectSpace.GetSequenceStorage(typeof(TestObject3)));
	        }

	        AssertNextSequences(application, 2,nextSequenceTest);
        }
        
        [Test][XpandTest]
        public void Invalid_length_Sequence_Registration(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        Should.Throw<NotSupportedException>(() => {
		        using var objectSpace = application.CreateObjectSpace();
		        objectSpace.SetSequence<LongFullNameTestObject>(o => o.SequentialNumber,Type.EmptyTypes.FirstOrDefault());
	        });
        }

        [Test][XpandTest]
        public void Not_Registered_Custom_Sequence_Type_Registration(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        using var objectSpace = application.CreateObjectSpace();
	        objectSpace.Delete(objectSpace.GetObjectsQuery<SequenceStorage>().ToArray());
	        objectSpace.CommitChanges();

	        Should.Throw<InvalidOperationException>(() =>
		        objectSpace.SetSequence<TestObject3>(_ => _.SequentialNumber, typeof(TestObject2)));
        }
        
        [Test][XpandTest]
        public void Invalid_Custom_Sequence_Type_Member_Registration(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        using var objectSpace = application.CreateObjectSpace();
	        Should.Throw<MemberNotFoundException>(() =>
		        objectSpace.SetSequence(typeof(TestObject3),Guid.NewGuid().ToString(), typeof(TestObject2).FullName));
	        Should.Throw<InvalidCastException>(() =>
		        objectSpace.SetSequence(typeof(TestObject3),nameof(TestObject2.Title), typeof(TestObject2).FullName));
        }
        
        [Test][XpandTest()]
        public void Update_Sequence_Type_Registration(){
	        using var application = SequenceGeneratorModule().Application;
	        SetSequences(application);
	        using var objectSpace = application.CreateObjectSpace();
	        objectSpace.SetSequence<TestObject3>(_ => _.SequentialNumber, typeof(TestObject));
	        objectSpace.GetSequenceStorage(typeof(TestObject3),false).CustomSequence.ShouldBe(typeof(TestObject).FullName);
        }

        [Test]
        [XpandTest()]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
        public async Task Generate_Sequences_When_Locks_From_ExplicitUow(){
	        using var application = NewApplication();
	        var simpleDataLayer = NewSimpleDataLayer(application);
	        SequenceGeneratorModule(application);
	        using (var objectSpace = application.CreateObjectSpace()){
		        objectSpace.SetSequence<TestObject>(o => o.SequentialNumber,10);
	        }
	        var testObjectObserver = SequenceGeneratorService.Sequence.OfType<TestObject>().SubscribeReplay();
	        var explicitUnitOfWork = new ExplicitUnitOfWork(simpleDataLayer);
                
	        new TestObject(explicitUnitOfWork);
	        explicitUnitOfWork.FlushChanges();
	        await TestObjects(application, false, 1).Merge(Unit.Default.ReturnObservable().Delay(TimeSpan.FromMilliseconds(300)).Do(unit => explicitUnitOfWork.CommitChanges())).FirstAsync();
	        explicitUnitOfWork.Close();
	        simpleDataLayer.Dispose();
                
	        var firstAsync = await testObjectObserver.FirstAsync();
	        firstAsync.SequentialNumber.ShouldBe(10);
        }


        [XpandTest()][Test]
        public async Task Custom_First_Sequence(){
	        using var application = SequenceGeneratorModule().Application;
	        using (var objectSpace = application.CreateObjectSpace()){
		        objectSpace.SetSequence<TestObject>(o => o.SequentialNumber,10);
	        }

	        var testObserver = SequenceGeneratorService.Sequence.OfType<TestObject>().Test();
            
	        await TestObjects(application, false, 1);
            
	        testObserver.Items.First().SequentialNumber.ShouldBe(10);

	        using (var objectSpace = application.CreateObjectSpace()){
		        objectSpace.SetSequence<TestObject>(o => o.SequentialNumber,20);
	        }
            
	        await TestObjects(application, false, 1);
            
	        testObserver.Items.Last().SequentialNumber.ShouldBe(20);
            
	        using (var objectSpace = application.CreateObjectSpace()){
		        objectSpace.SetSequence<TestObject>(o => o.SequentialNumber,1);
	        }
            
	        await TestObjects(application, false, 1);
            
	        testObserver.Items.Last().SequentialNumber.ShouldBe(21);
        }

        [Test][XpandTest]
        public async Task SecuredObjectSpaceProvider_Installed(){
	        using var application = NewApplication();
	        var securityStrategyComplex = new SecurityStrategyComplex(typeof(PermissionPolicyUser), typeof(PermissionPolicyRole), new AuthenticationStandard());
	        securityStrategyComplex.AnonymousAllowedTypes.Add(typeof(TestObject));
	        application.Security= securityStrategyComplex;
	        SequenceGeneratorModule( application);
	        SetSequences(application);

	        var testObserver = SequenceGeneratorService.Sequence.OfType<TestObject>().Test();
                
	        await TestObjects(application, false, 1);
                
	        AssertNextSequences(application, 1,testObserver);
        }
        [Test][XpandTest]
        public void Generate_Sequence_When_Error_Inside_transaction(){
            using var application = NewApplication();
            application.Modules.AddRange(new ModuleBase[] {new DevExpress.ExpressApp.Validation.ValidationModule(),new ValidationWindowsFormsModule() });
            application.WhenApplicationModulesManager().WhenCustomizeTypesInfo()
                .Do(t => t.e.TypesInfo.FindTypeInfo(typeof(TestObject)).FindMember(nameof(TestObject.Title))
                    .AddAttribute(new RuleRequiredFieldAttribute())).Test();
            SequenceGeneratorModule( application);
            SetSequences(application);
            var testObserver = SequenceGeneratorService.Sequence.OfType<TestObject>().Test();
            var compositeView = application.NewView<DetailView>(typeof(TestObject));
            compositeView.CurrentObject = compositeView.ObjectSpace.CreateObject<TestObject>();
            application.CreateViewWindow().SetView(compositeView);
            var testObject = ((TestObject) compositeView.CurrentObject);
            Should.Throw<ValidationException>(() => compositeView.ObjectSpace.CommitChanges());
            testObject.Title = "test";
            compositeView.ObjectSpace.CommitChanges();

			testObserver.ItemCount.ShouldBe(1);
			testObserver.Items.First().ShouldBe(testObject);
        }
    }

}