using System.Linq;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.Email.Tests.BOModel;
using Xpand.XAF.Modules.Email.Tests.Common;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Email.Tests {
    public class ModelLogicTests : CommonAppTest {
        private IModelEmail _modelEmail;
        private IModelEmailRule _modelEmailRule            ;

        private IModelEmailObjectView _modelEmailObjectView;

        public override void Init() {
            base.Init();
            _modelEmail = Application.Model.ToReactiveModule<IModelReactiveModulesEmail>().Email;
            _modelEmailRule = _modelEmail.Rules.AddNode<IModelEmailRule>();
            _modelEmailRule.Type = _modelEmailRule.Application.BOModel.GetClass(typeof(E));
        }

        [Test][Order(0)]
        public void ObjectViews_List_ParentClass_Views() {
            _modelEmailObjectView = _modelEmailRule.ObjectViews.AddNode<IModelEmailObjectView>();
            
            _modelEmailObjectView.ObjectViews.Count.ShouldBe(3);
            _modelEmailObjectView.ObjectViews.ShouldContain(_modelEmailRule.Type.DefaultDetailView);
        }

        [Test][Order(100)]
        public void String_Members_Lists_ObjectView_Members() {
            _modelEmailObjectView.ObjectView = _modelEmailObjectView.ObjectViews.First();
            
            _modelEmailObjectView.StringMembers.ShouldContain(_modelEmailObjectView.ObjectView.ModelClass.FindMember(nameof(E.Name)));
        }
        
        [Test][Order(100)]
        public void UserEmailMember() {
            _modelEmailObjectView.StringMembers.ShouldContain(_modelEmailObjectView.ObjectView.ModelClass.FindMember(nameof(E.Name)));
        }


    }
}