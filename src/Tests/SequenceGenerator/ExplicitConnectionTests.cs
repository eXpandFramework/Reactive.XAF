using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.Xpo;
using NUnit.Framework;
using Shouldly;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests{
    public class ExplicitConnectionTests:SequenceGeneratorTestsBaseTests{
        [XpandTest()]
        [Test()]
        public void Throws_if_another_connection_exists_with_same_datalayer(){
            using (var application = SequenceGeneratorModule(nameof(Throws_if_another_connection_exists_with_same_datalayer)).Application){
                using (var defaultDataLayer = NewSimpleDataLayer(application)){
                    var explicitUnitOfWork = new ExplicitUnitOfWork(defaultDataLayer);
                    var testObject = new TestObject(explicitUnitOfWork);
                    explicitUnitOfWork.FlushChanges();
                    explicitUnitOfWork.GetObjectByKey<TestObject>(testObject.Oid).ShouldNotBeNull();

                    var explicitUnitOfWork1 = new ExplicitUnitOfWork(defaultDataLayer);

                    // ReSharper disable once ObjectCreationAsStatement
                    new TestObject2(explicitUnitOfWork1);

                    Should.Throw<InvalidOperationException>(() => explicitUnitOfWork1.FlushChanges(), SequenceGeneratorService.ParallelTransactionExceptionMessage);

                    explicitUnitOfWork.Close();
                    explicitUnitOfWork1.Close();
                }

            }
        }

        [Test][Ignore("Passed in the past, maybe some side effect of xpo")]
        public void Locks_Only_Modified_Tables_When_Different_datalyer(){
            using (var application = SequenceGeneratorModule(nameof(Locks_Only_Modified_Tables_When_Different_datalyer)).Application){
                var defaultDataLayer1 = NewSimpleDataLayer(application);
                var explicitUnitOfWork = new ExplicitUnitOfWork(defaultDataLayer1);
                var testObject = new TestObject(explicitUnitOfWork);
                explicitUnitOfWork.FlushChanges();

                var defaultDataLayer2 = NewSimpleDataLayer(application);
                var explicitUnitOfWork1 = new ExplicitUnitOfWork(defaultDataLayer2);
                var testObject2 = new TestObject2(explicitUnitOfWork1);
                explicitUnitOfWork1.FlushChanges();
            
                Should.Throw<ShouldCompleteInException>(() => Should.CompleteIn(
                    () => explicitUnitOfWork.GetObjectByKey<TestObject2>(testObject2.Oid).ShouldNotBeNull(),
                    TimeSpan.FromSeconds(1)));
                Should.Throw<ShouldCompleteInException>(() => Should.CompleteIn(
                    () => explicitUnitOfWork1.GetObjectByKey<TestObject>(testObject.Oid).ShouldNotBeNull(),
                    TimeSpan.FromSeconds(1)));
                explicitUnitOfWork.Close();
                defaultDataLayer1.Dispose();
                explicitUnitOfWork1.Close();
                defaultDataLayer2.Dispose();
            }
        }

        [Test]
        [XpandTest()]
        public async Task UnLocks_current_Record_When_Commit_Changes(){
            using (var application = SequenceGeneratorModule(nameof(UnLocks_current_Record_When_Commit_Changes)).Application){
                var defaultDataLayer1 = await application.ObjectSpaceProvider.SequenceGeneratorDatalayer();
                
                var explicitUnitOfWork = new ExplicitUnitOfWork(defaultDataLayer1);
            
                explicitUnitOfWork.SetSequence<TestObject>( o => o.SequentialNumber);
                explicitUnitOfWork.FlushChanges();

                var defaultDataLayer2 = NewSimpleDataLayer(application);
                var explicitUnitOfWork1 = new ExplicitUnitOfWork(defaultDataLayer2);

                explicitUnitOfWork.CommitChanges();
                explicitUnitOfWork1.GetSequenceStorage(typeof(TestObject));
            
                explicitUnitOfWork.Close();
                defaultDataLayer1.Dispose();
                explicitUnitOfWork1.Close();
                defaultDataLayer2.Dispose();
            }
        }
        
    }
}