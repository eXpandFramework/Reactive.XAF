using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
    public class ModelLogicTests:PositionInListViewCommonTest{
        [Test][XpandTest()][Order(1)]
        public void ModelListView_datasource_contains_only_views_with_int_Members() {
            using var application=PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
            positionInListView.ListViews.Count().ShouldBeGreaterThanOrEqualTo(1);
            positionInListView.ListViews.Select(view => view.ModelClass.TypeInfo.Type).ShouldContain(typeof(PIL));
            positionInListView.ListViews.All(view =>
                view.ModelClass.AllMembers.Any(member => member.Type == typeof(int)||member.Type == typeof(long))).ShouldBeTrue();
        }
        [Test][XpandTest()][Order(1)]
        public void ListViewModelMember_datasource_contains_int_Members() {
            using var application=PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
            positionInListView.ListView = application.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
            positionInListView.PositionMembers.Count().ShouldBe(1);
            positionInListView.PositionMembers.First().Id().ShouldBe(nameof(PIL.Order));
        }
        [Test][XpandTest()][Order(2)]
        public void ModelListViewPositionMember_defaults_to_first_long_member() {
            using var application=PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
            positionInListView.ListView = application.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
            positionInListView.PositionMember.ShouldNotBeNull();
            positionInListView.PositionMember.Id().ShouldBe(nameof(PIL.Order));
        }
        [Test][XpandTest()][Order(1)]
        public void ModelClass_datasource_contains_only_classes_with_int_Members() {
            using var application=PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ModelClassItems.AddNode<IModelPositionInListViewModelClassItem>();
            positionInListView.ModelClasses.Count().ShouldBeGreaterThanOrEqualTo(1);
            positionInListView.ModelClasses.Select(modelClass => modelClass.TypeInfo.Type).ShouldContain(typeof(PIL));
            positionInListView.ModelClasses.All(modelClass =>
                modelClass.AllMembers.Any(member => member.Type == typeof(int)||member.Type == typeof(long))).ShouldBeTrue();
        }

        [Test][XpandTest()][Order(1)]
        public void ModelClassModelMember_datasource_contains_int_modelers() {
            using var application=PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ModelClassItems.AddNode<IModelPositionInListViewModelClassItem>();
            positionInListView.ModelClass = application.Model.BOModel.GetClass(typeof(PIL));
            positionInListView.ModelMembers.Count().ShouldBe(1);
            positionInListView.ModelMembers.First().Id().ShouldBe(nameof(PIL.Order));
        }

        [Test][XpandTest()][Order(2)]
        public void ModelClassMember_defaults_to_first_long_member() {
            using var application=PositionInListViewModuleModule().Application;
            var positionInListView = application.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
                .PositionInListView.ModelClassItems.AddNode<IModelPositionInListViewModelClassItem>();
            positionInListView.ModelClass = application.Model.BOModel.GetClass(typeof(PIL));
            positionInListView.ModelMember.ShouldNotBeNull();
            positionInListView.ModelMember.Id().ShouldBe(nameof(PIL.Order));
        }

    }
}