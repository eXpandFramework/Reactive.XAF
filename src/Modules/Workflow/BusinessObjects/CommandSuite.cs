using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Humanizer;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Workflow.BusinessObjects.Commands;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects{
    [DefaultProperty(nameof(Name))]
    [DefaultClassOptions][ImageName("CommandSuite")]
    [ListViewShowFooter]
    [CloneModelView(CloneViewType.DetailView, CommandSuiteEnableDisableDetailView)]
    [NavigationItemQuickAccess]
    public class CommandSuite(Session session) : XPCustomBaseObject(session),IActiveWorkflowObject{
        public const string CommandSuiteEnableDisableDetailView = "CommandSuiteEnableDisable_DetailView";
        [ToolTip("Globally enables success notifications for all commands in this suite. A command's individual 'Notify Emission' setting can also enable notifications even if this is false.")]
        public bool NotifyEmission{
            get => _notifyEmission;
            set => SetPropertyValue(nameof(NotifyEmission), ref _notifyEmission, value);
        }

        [ToolTip("Sets a default duration for how long success notifications are displayed for all commands in this suite.")]
        public TimeSpan? HideNotification{
            get => _hideNotification;
            set => SetPropertyValue(nameof(HideNotification), ref _hideNotification, value);
        }
        
        [EditorAlias(EditorAliases.LabelPropertyEditor)]
        public string LastExecution => Commands
            .SelectMany(command => command.Executions.OrderByDescending(execution => execution.Created)).FirstOrDefault()?
            .Created.Humanize(utcDate:false);
        bool _active;
        
        [ToolTip("The master switch for the entire suite. If false, no commands within this suite will be scheduled or executed, regardless of their individual 'Active' status.")]
        public bool Active{
            get => _active;
            set => SetPropertyValue(nameof(Active), ref _active, value);
        }

        int IActiveWorkflowObject.Index => 0;

        [Association("CommandSuite-ActionObjects")][Aggregated]
        public XPCollection<WorkflowCommand> Commands => GetCollection<WorkflowCommand>();

        Guid _oid;

        [Key][Browsable(false)]
        public Guid Oid{
            get => _oid;
            set => SetPropertyValue(nameof(Oid), ref _oid, value);
        }


        string _name;
        private bool _notifyEmission;
        private bool _appNotification;
        private bool _verboseNotification;
        private TimeSpan? _hideNotification;

        [RuleRequiredField]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }

        
        public override void AfterConstruction(){
            base.AfterConstruction();
            ClassInfo.KeyProperty.SetValue(this,XpoDefault.NewGuid());
            NotifyEmission = true;
            HideNotification = 10.Seconds();
            Active = true;
            AppNotification = true;
        }


        [ToolTip("Controls whether notifications are displayed within the application's standard UI. If false, notifications will only be sent to other channels like Telegram, if configured.")]
        public bool AppNotification{
            get => _appNotification;
            set => SetPropertyValue(nameof(AppNotification), ref _appNotification, value);
        }

        [ToolTip("If true, notifications will include additional context, such as the name of the Command Suite and the specific Command that executed.")]
        public bool VerboseNotification{
            get => _verboseNotification;
            set => SetPropertyValue(nameof(VerboseNotification), ref _verboseNotification, value);
        }
    }
}