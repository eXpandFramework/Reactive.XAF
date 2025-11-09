using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Drawing;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Humanizer;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Attributes.Appearance;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.CloneModelView;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    public interface IActiveWorkflowObject:IObjectSpaceLink{
        bool Active{ get; set; }
        int Index{ get; }
    }

    [DefaultProperty(nameof(Description))]
    [AppearanceToolTip("Delete action",AppearanceItemType.Action,nameof(Type)+"='"+nameof(ActionObjectType.System)+"'",TargetItems = "Delete",Enabled = false)]
    [ImageName("Command")]
    [OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    [AppearanceToolTip("Bold last",AppearanceItemType.ViewItem, nameof(IsSource)+"=0",TargetItems = "*",FontStyle = DXFontStyle.Bold)]
    [System.ComponentModel.DisplayName("Action")]
    [CloneModelView(CloneViewType.DetailView, ActionObjectExecutionsDetailView)]
    [ListViewShowFooter]
    [ShowInstanceDetailView()]
    public abstract class WorkflowCommand(Session session) : XPCustomBaseObject(session), IActiveWorkflowObject,IDefaultProperty{

        public const string ActionObjectExecutionsDetailView = "ActionObjectExecutions_DetailView";
        WorkflowCommand _startAction;

        [RuleRequiredField(TargetCriteria = nameof(NeedSubscription)+"=1")]
        [DataSourceProperty(nameof(Commands))]
        [InvisibleInAllListViews]
        public WorkflowCommand StartAction{
            get => _startAction;
            set => SetPropertyValue(ref _startAction, value);
        }

        [InvisibleInAllViews]
        public string FullName => $"{CommandSuite} {Description}";
        [Association("Command-StartActions")][DataSourceProperty(nameof(Commands))]
        public XPCollection<WorkflowCommand> StartCommands => GetCollection<WorkflowCommand>();

        WorkflowCommand _startWorkflowCommand;

        [Association("Command-StartActions")][HideInUI(HideInUI.All)]
        public WorkflowCommand StartWorkflowCommand{
            get => _startWorkflowCommand;
            set => SetPropertyValue(nameof(StartWorkflowCommand), ref _startWorkflowCommand, value);
        }
        
        [Browsable(false)]
        public List<WorkflowCommand> Commands => CommandSuite.Commands.Where(command => !ReferenceEquals(command, this)).ToList();
        public int Index => GetIndex();

        protected internal virtual int GetIndex(){
            var startActions = new[] { StartAction }
                .WhereNotDefault()
                .Concat(StartCommands.WhereNotDefault()).ToArray();
            return startActions.Any() ? 1 + startActions.Max(c => c.GetIndex()) : 0;
        }

        bool _logExecutions;

        [VisibleInListView(false)]
        public bool LogExecutions{
            get => _logExecutions;
            set => SetPropertyValue(nameof(LogExecutions), ref _logExecutions, value);
        }

        [InvisibleInAllViews]
        public bool NeedSubscription => GetNeedSubscription();

        protected virtual bool GetNeedSubscription() => true;

        public abstract IObservable<object[]> Execute(XafApplication application, params object[] objects); 

        [VisibleInDetailView(false)]
        public string StartPath => GetStartPath();
        public virtual string GetStartPath() => $"{StartAction?.Name}=> {StartAction?.Description}";

        DateTime _created;

        [Browsable(false)]
        public DateTime Created{
            get => _created;
            set => SetPropertyValue(nameof(Created), ref _created, value);
        }
        
        Guid _oid;

        [Key][Browsable(false)]
        public Guid Oid{
            get => _oid;
            set => SetPropertyValue(nameof(Oid), ref _oid, value);
        }

        
        [VisibleInLookupListView(true)][ColumnSummary(SummaryType.Count)][Index(0)][EditorAlias(EditorAliases.LabelPropertyEditor)]
        [VisibleInDetailView(false)]
        public string Name => CaptionHelper.ApplicationModel?.BOModel?.GetClass(GetType())?.Caption??GetType().Name.CompoundName();
        
        
        public override void AfterConstruction(){
            base.AfterConstruction();
            ClassInfo.KeyProperty.SetValue(this,XpoDefault.NewGuid());
            Active = true;
            Created=DateTime.Now;
            HideNotification = TimeSpan.FromSeconds(10);
            
        }

        [InvisibleInAllViews]
        public bool IsSource => !Active|| (CommandSuite?.Commands.Where(command => command.Active)
            .SelectMany(o => o.StartCommands.AddToArray(o.StartAction)).WhereNotDefault()
            .Any(command =>command.Active&& command.Oid == Oid) ?? false);

        CommandSuite _commandSuite;

        [Association("CommandSuite-ActionObjects")]
        public CommandSuite CommandSuite{
            get => _commandSuite;
            set => SetPropertyValue(nameof(CommandSuite), ref _commandSuite, value);
        }
        
        [Size(-1)][VisibleInDetailView(false)][Index(1)]
        public string Description => GetDescription();

        protected virtual string GetDescription() 
            => $"{Name}->{MembersString()}";

        private string MembersString() 
            =>IsInvalidated?null: GetType().ToTypeInfo().OwnMembers.Where(info => info.IsPublic&&!info.IsList)
                .Where(info => !info.FindAttributes<BrowsableAttribute>().Any()).OrderBy(info => info.Name)
                .Select(info => {
                    var value = info.GetValue(this);
                    if (info.MemberType.DefaultValue() == value)return null;
                        
                    value = value switch{
                        TimeSpan timeSpan => timeSpan.Humanize(),
                        DateTime dateTime => dateTime.HumanizeCompact(),
                        Type type => type.Name,
                        decimal d => d.Normalize(),
                        _ => value
                    };
                    return $"{info.Name}: {value}";
                }).WhereNotDefault().JoinCommaSpace();


        [InvisibleInAllViews]
        public ActionObjectType Type => GetActionObjectType();

        protected virtual ActionObjectType GetActionObjectType() => ActionObjectType.User;

        [EditorAlias(EditorAliases.LabelPropertyEditor)][VisibleInListView(false)]
        public string Returns => ReturnType?.GetModelClass()?.Caption??ReturnType?.Name;
        [InvisibleInAllViews]
        public Type ReturnType => GetReturnType();

        protected virtual Type GetReturnType() => StartAction?.ReturnType;

        bool _active;

        public bool Active{
            get => _active;
            set => SetPropertyValue(ref _active, value);
        }

        

        bool _executeOnce;

        bool _notifyEmission;

        public bool NotifyEmission{
            get => _notifyEmission;
            set => SetPropertyValue(nameof(NotifyEmission), ref _notifyEmission, value);
        }

        TimeSpan? _hideNotification;

        [VisibleInListView(false)]
        public TimeSpan? HideNotification{
            get => _hideNotification;
            set => SetPropertyValue(nameof(HideNotification), ref _hideNotification, value);
        }
        
        public bool ExecuteOnce{
            get => _executeOnce;
            set => SetPropertyValue(nameof(ExecuteOnce), ref _executeOnce, value);
        }

        [Association("CommandObject-ActionObjectExecutions")][Aggregated][ReadOnlyCollection]
        public XPCollection<CommandExecution> Executions => GetCollection<CommandExecution>();

        object IDefaultProperty.DefaultPropertyValue => $"{Name}: {Description}";

        bool _disableOnError;

        public bool DisableOnError{
            get => _disableOnError;
            set => SetPropertyValue(nameof(DisableOnError), ref _disableOnError, value);
        }

        public virtual bool ShouldLogExecutions() => LogExecutions;

        [Association("CommandCategory-Commands")][InvisibleInAllViews]
        public XPCollection<CommandCategory> Categories => GetCollection<CommandCategory>();

        [Browsable(false)]
        public bool HasCriteria => ClassInfo.Members.Any(info => info.HasAttribute(typeof(CriteriaOptionsAttribute)));
    }
    

    public enum ActionObjectType{
        System,
        User
    }
}
