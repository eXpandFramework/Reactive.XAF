using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Workflow.Services{
    internal static class ObjectExistService{
        internal static IObservable<object[]> InvokeObjectExistCommand(this ObjectExistWorkflowCommand workflowCommand,XafApplication application, params object[] objects){
            LogFast($"Entering {nameof(InvokeObjectExistCommand)} for command '{workflowCommand.FullName}' with {objects.Length} input objects.");
            if (workflowCommand.ObjectName==null){
                LogFast($"Command '{workflowCommand.FullName}' has a null ObjectName. Exiting.");
                return Observable.Empty<object[]>();
            }
            var typeInfo = workflowCommand.ObjectSpace.TypesInfo.FindTypeInfo(workflowCommand.ObjectName);
            var criteria = GetCriteria(workflowCommand ,objects);
            LogFast($"Resolved Type: '{typeInfo.FullName}'. Generated Criteria: '{criteria}'.");
            var existing= application.UseProviderObjectSpace(space
                => GetObjects(space.GetObject(workflowCommand),criteria).ToArray().Observe(),typeInfo.Type).WhenNotEmpty();
            var commits= application.WhenProviderCommitted(typeInfo.Type,workflowCommand.ObjectModification)
                .Select(t => t.objects.Where(o => typeInfo.Type.IsInstanceOfType(o))
                    .Where(o => t.objectSpace.IsObjectFitForCriteria( criteria, o)).ToArray())
                .WhenNotEmpty();
            switch (workflowCommand.SearchMode){
                case CommandSearchMode.Existing:
                    LogFast($"SearchMode is '{workflowCommand.SearchMode}'. Ignoring commits and only searching existing objects.");
                    commits= Observable.Empty<object[]>();
                    break;
                case CommandSearchMode.Commits:{
                    LogFast($"SearchMode is '{workflowCommand.SearchMode}'. Ignoring existing objects and only listening for commits.");
                    existing= Observable.Empty<object[]>();
                    commits = commits.SkipOrOriginal(workflowCommand.SkipTopReturnObjects)
                        .TakeOrOriginal(workflowCommand.TopReturnObjects);
                    break;
                }
                default:{
                    LogFast($"SearchMode is '{workflowCommand.SearchMode}'. Searching existing objects and listening for commits.");
                    commits = commits.SkipOrOriginal(workflowCommand.SkipTopReturnObjects)
                        .TakeOrOriginal(workflowCommand.TopReturnObjects);
                    break;
                }
            }

            return existing.Merge(commits)
                .Do(output => LogFast($"{nameof(InvokeObjectExistCommand)} is emitting an array with {output.Length} items."))
                .Select(workflowCommand.ToOutputValue);
        }
        internal static object[] ToOutputValue(this ObjectExistWorkflowCommand objectExistWorkflowCommand,params object[] objects){
            var typeInfo = objectExistWorkflowCommand.CriteriaType?.ToTypeInfo();
            if (typeInfo == null || !objects.Any() || objectExistWorkflowCommand.OutputProperty.IsNullOrEmpty()) {
                return objects;
            }

            var outputProperty = objectExistWorkflowCommand.OutputProperty;

            if (outputProperty.Contains('{') && outputProperty.Contains('}')) {
                var regex = new System.Text.RegularExpressions.Regex(@"\{(.+?)\}");
                return objects.Select(obj => {
                    return regex.Replace(outputProperty, match => {
                        var propertyName = match.Groups[1].Value;
                        var member = typeInfo.FindMember(propertyName);
                        var value = member?.GetValue(obj);
                        return value?.ToString() ?? string.Empty;
                    });
                }).WhereNotDefault().Distinct().Cast<object>().ToArray();
            }

            var memberInfo = typeInfo.FindMember(outputProperty);
            return memberInfo == null ? []
                : objects.Select(o => memberInfo.GetValue(o)).WhereNotDefault().Distinct().ToArray();
        }

        internal static IEnumerable<object> GetObjects(this ObjectExistWorkflowCommand workflowCommand,CriteriaOperator criteriaOperator) 
            => workflowCommand.ObjectSpace.GetObjects(workflowCommand.ObjectSpace.TypesInfo.FindTypeInfo(workflowCommand.ObjectName).Type,
                criteriaOperator, workflowCommand.TopReturnObjects, workflowCommand.SkipTopReturnObjects,workflowCommand.SortProperties
                    .Select(property => new SortProperty(property.Name,property.Direction)).ToArray()).Cast<object>();
        internal static CriteriaOperator GetCriteria(this ObjectExistWorkflowCommand objectExistWorkflowCommand, object[] objects){
            var criteriaOperator = objectExistWorkflowCommand.ObjectSpace.ParseCriteria(objectExistWorkflowCommand.Criteria);
            var propertyName = objectExistWorkflowCommand.InputFilterProperty??objectExistWorkflowCommand.CriteriaType.ToTypeInfo().KeyMember.Name;
            var memberType = objectExistWorkflowCommand.CriteriaType.ToTypeInfo().FindMember(propertyName)?.MemberType;
            if (memberType == null)return criteriaOperator;
            if (objects.WhereNotDefault().Any(o => memberType.IsAssignableFrom(o.GetType()))){
                var inOperator = new InOperator(propertyName, objects);
                if (!criteriaOperator.ReferenceEquals(null)){
                    criteriaOperator=new GroupOperator(GroupOperatorType.And,criteriaOperator,inOperator);    
                }
                else{
                    criteriaOperator = inOperator;
                }
            }
            return criteriaOperator;
        }

    }
}