using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo.BaseObjects;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [DefaultProperty(nameof(ObjectExistWorkflowCommand))]
    public class CommandSortProperty(Session session) : XPCustomBaseObject(session){
        private Guid _oid;

        [Key][Browsable(false)]
        public Guid Oid{
            get => _oid;
            set => SetPropertyValue(nameof(Oid), ref _oid, value);
        }
        
        public override void AfterConstruction(){
            base.AfterConstruction();
            ClassInfo.KeyProperty.SetValue(this,XpoDefault.NewGuid());
            Direction=SortingDirection.Ascending;
        }

        string _name;

        [RuleRequiredField]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        SortingDirection _direction;
        public SortingDirection Direction{
            get => _direction;
            set => SetPropertyValue(nameof(Direction), ref _direction, value);
        }

        ObjectExistWorkflowCommand _objectExistWorkflowCommand;

        [Association("ObjectExistCommand-CommandSortProperties")][RuleRequiredField]
        [InvisibleInAllViews]public ObjectExistWorkflowCommand ObjectExistWorkflowCommand{
            get => _objectExistWorkflowCommand;
            set => SetPropertyValue(nameof(ObjectExistWorkflowCommand), ref _objectExistWorkflowCommand, value);
        }
    }
    
    [CodeRule]
    public class CommandSortPropertyExistRule : RuleBase<CommandSortProperty>{

        public CommandSortPropertyExistRule():base("","Save"){
        }

        public CommandSortPropertyExistRule(IRuleBaseProperties properties) : base(properties){
        }

        public override ReadOnlyCollection<string> UsedProperties => new([nameof(CommandSortProperty.Name)]);

        protected override bool IsValidInternal(CommandSortProperty target, out string errorMessage) {
            errorMessage = $"CommandSortProperty {target.Name} not found on {target}";
            var criteriaType = target.ObjectExistWorkflowCommand.CriteriaType;
            return criteriaType != null &&
                   target.ObjectSpace.TypesInfo.FindTypeInfo(criteriaType)?.FindMember(target.Name) != null;
        }
        
               
    }
    [CodeRule]
    public class StartCommandCircularDependencyRule : RuleBase<WorkflowCommand> {
        public StartCommandCircularDependencyRule() : base("", "Save")
            => Properties.SkipNullOrEmptyValues = false;

        public StartCommandCircularDependencyRule(IRuleBaseProperties properties) : base(properties) { }
        
        public override ReadOnlyCollection<string> UsedProperties => new([
            nameof(WorkflowCommand.StartAction),
            nameof(WorkflowCommand.StartCommands)
        ]);

        protected override bool IsValidInternal(WorkflowCommand target, out string errorMessage) {
            if (HasCircularDependency(target, new HashSet<WorkflowCommand>())) {
                errorMessage = "A circular dependency was detected in the command chain.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        private bool HasCircularDependency(WorkflowCommand currentNode, HashSet<WorkflowCommand> visitedNodes) {
            if (currentNode == null) {
                return false;
            }

            if (!visitedNodes.Add(currentNode)) {
                return true;
            }

            var dependencies = new List<WorkflowCommand>();
            if (currentNode.StartAction != null) {
                dependencies.Add(currentNode.StartAction);
            }
            dependencies.AddRange(currentNode.StartCommands);

            foreach (var dependency in dependencies.Where(dep => dep != null)) {
                if (HasCircularDependency(dependency, visitedNodes)) {
                    return true;
                }
            }

            visitedNodes.Remove(currentNode);
            return false;
        }
    }
}