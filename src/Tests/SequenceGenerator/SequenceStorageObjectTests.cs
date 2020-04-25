using System;
using System.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.TypesInfo;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.SequenceGenerator.Tests.BO;

namespace Xpand.XAF.Modules.SequenceGenerator.Tests{
    public class SequenceStorageObjectTests:SequenceGeneratorTestsBaseTests{
        [Test][XpandTest()]
        public void SequenceStorageObjectType_Lookup_should_list_all_persistent_types(){
            using (var application = SequenceGeneratorModule(nameof(SequenceStorageObjectType_Lookup_should_list_all_persistent_types)).Application){
                using (var objectSpace = application.CreateObjectSpace()){
                    var sequenceStorage = objectSpace.CreateObject<SequenceStorage>();
                    sequenceStorage.Types.Count.ShouldBeGreaterThan(0);
                    sequenceStorage.Types.All(type => type.Type.ToTypeInfo().IsPersistent).ShouldBeTrue();
                }
            }
        }
        
        [Test]
        public void SequenceStorage_Member_Lookup_should_list_all__long_ObjectType_types(){
            using (var application = SequenceGeneratorModule(nameof(SequenceStorage_Member_Lookup_should_list_all__long_ObjectType_types)).Application){
                using (var objectSpace = application.CreateObjectSpace()){
                    var sequenceStorage = objectSpace.CreateObject<SequenceStorage>();
                    sequenceStorage.Type=new ObjectType(){Type = typeof(TestObject)};
                
                    sequenceStorage.Members.Count.ShouldBe(1);
                    sequenceStorage.Members.Select(member => member.Name).First().ShouldBe(nameof(TestObject.SequentialNumber));
                }

            }
        }
        
        [Test]
        public void SequenceStorageCustomSequence_Lookup_should_list_all_sequence_base_types(){
            using (var application = SequenceGeneratorModule(nameof(SequenceStorageCustomSequence_Lookup_should_list_all_sequence_base_types)).Application){
                SetSequences(application);
                using (var objectSpace = application.CreateObjectSpace()){
                    var storage =(SequenceStorage) objectSpace.GetSequenceStorage(typeof(TestObject3));
                    storage.Type=new ObjectType(){Type = typeof(TestObject3)};
                    storage.CustomTypes.Select(type => type.Type).ShouldNotContain(typeof(TestObject3));
                    storage.CustomTypes.Select(type => type.Type).ShouldContain(typeof(TestObject2));
                }
            }
        }

        
        [Test]
        public void Create_New_SequenceStorage_From_DetailView(){
            using (var application = SequenceGeneratorModule(nameof(Create_New_SequenceStorage_From_DetailView)).Application){
                SetSequences(application);
                using (var window = application.CreateViewWindow()){
                    using (var objectSpace = application.CreateObjectSpace()){
                        var sequenceStorage = objectSpace.CreateObject<SequenceStorage>();
                        window.SetView(application.CreateDetailView(sequenceStorage));
                    }
                }
            }
        }

        [TestCase(typeof(ExplicitUnitOfWork))]
        [TestCase(typeof(UnitOfWork))]
        public void SequenceStorage_UI_Properties_are_configured_on_load(Type uowType){
            using (var application = SequenceGeneratorModule(nameof(SequenceStorage_UI_Properties_are_configured_on_load)).Application){
                SetSequences(application);
                using (application.CreateObjectSpace()){
                    IDataLayer dataLayer = ((XPObjectSpaceProvider) application.ObjectSpaceProvider).DataLayer;
                    using (var unitOfWork = (UnitOfWork)Activator.CreateInstance(uowType,dataLayer)){
                        var sequenceStorage = unitOfWork.Query<SequenceStorage>().ToArray()
                            .First(storage => storage.Name == typeof(TestObject3).FullName);
                        if (uowType!=typeof(ExplicitUnitOfWork)){
                            sequenceStorage.Type.Type.FullName.ShouldBe(sequenceStorage.Name);
                            sequenceStorage.Member.Name.ShouldBe(sequenceStorage.SequenceMember);
                            sequenceStorage.CustomType.Type.FullName.ShouldBe(sequenceStorage.CustomSequence);
                        }
                        else{
                            sequenceStorage.Type.ShouldBeNull();
                            sequenceStorage.CustomType.ShouldBeNull();
                        }
                    }
                }

            }
        }

        [TestCase(typeof(DetailView))]
        public void Configure_SequencStorage_When_ObjectSpace_Commits(Type objectViewType){
            Tracing.Close();
            var testObserver = new TestTracing().WhenException().Test();
            Tracing.Initialize();
            using (var application=NewApplication(Platform.Web)){
                SequenceGeneratorModule(nameof(Configure_SequencStorage_When_ObjectSpace_Commits), application);
                SetSequences(application);
                var modelClass = application.Model.BOModel.GetClass(typeof(SequenceStorage));
                var viewId = objectViewType == typeof(DetailView) ? modelClass.DefaultDetailView.Id : modelClass.DefaultListView.Id;
                var compositeView = application.NewView(application.FindModelView(viewId));
                var sequenceStorage = compositeView.ObjectSpace.GetObjectsQuery<SequenceStorage>().First();
                sequenceStorage.Member = null;
                compositeView.ObjectSpace.CommitChanges();
                testObserver.Items.Count.ShouldBe(1);
                testObserver.Items.First().Message.ShouldContain("Cannot find the '' property within the ");
                compositeView.ObjectSpace.CommitChanges();
                testObserver.Items.Count.ShouldBe(2);
                testObserver.Items.Last().Message.ShouldContain("Cannot find the '' property within the ");
                sequenceStorage.Member = new ObjectMember(){Name = nameof(TestObject.SequentialNumber)};
                compositeView.ObjectSpace.CommitChanges();
                testObserver.Items.Count.ShouldBe(2);
            }
        }
    }

    
}