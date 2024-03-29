﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.SecurityExtensions;

namespace Xpand.XAF.Modules.Reactive.Logger{
    [DebuggerDisplay("{" + nameof(Location) + "}-{" + nameof(RXAction) + ("}-{" + nameof(Method) + "}"))]
    [DeferredDeletion(false)][OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    [NonSecuredType][CreatableItem(false)]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class TraceEvent : XPCustomObject,IObjectSpaceLink , ITraceEvent{
        public TraceEvent(Session session) : base(session){
        }

        string _sourceFilePath;

        [Size(SizeAttribute.Unlimited)]
        public string SourceFilePath {
            get => _sourceFilePath;
            set => SetPropertyValue(nameof(SourceFilePath), ref _sourceFilePath, value);
        }
        
        public override void AfterConstruction(){
            base.AfterConstruction();
            Called = 1;
        }

        [VisibleInListView(false), VisibleInDetailView(false), VisibleInLookupListView(false), Key(true)][NonCloneable]
        public long Index{ get; set; }

        string _resultType;
        [Size(255)]
        public string ResultType{
            get => _resultType;
            set => SetPropertyValue(nameof(ResultType), ref _resultType, value);
        }

        string _source;
        [Size(255)]
        public string Source{
            get => _source;
            set => SetPropertyValue(nameof(Source), ref _source, value);
        }

        TraceEventType _traceEventType;

        public TraceEventType TraceEventType{
            get => _traceEventType;
            set => SetPropertyValue(nameof(TraceEventType), ref _traceEventType, value);
        }

        string _location;
        [Size(255)]
        public string Location{
            get => _location;
            set => SetPropertyValue(nameof(Location), ref _location, value);
        }

        string _method;
        [Size(255)]
        public string Method{
            get => _method;
            set => SetPropertyValue(nameof(Method), ref _method, value);
        }

        int _line;

        public int Line{
            get => _line;
            set => SetPropertyValue(nameof(Line), ref _line, value);
        }

        string _value;
        [Size(-1)]
        public string Value{
            get => _value;
            set => SetPropertyValue(nameof(Value), ref _value, value);
        }

        string _action;
        
        public string Action{
            get => _action;
            set => SetPropertyValue(nameof(Action), ref _action, value);
        }

        protected override void OnSaving(){
            base.OnSaving();
            if (Session.IsNewObject(this)){
                Enum.TryParse(Action, out _rXAction);
            }
        }

        RXAction _rXAction;
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public RXAction RXAction{
            get => _rXAction;
            set => SetPropertyValue(nameof(RXAction), ref _rXAction, value);
        }

        string _message;
        [Size(-1)]
        public string Message{
            get => _message;
            set => SetPropertyValue(nameof(Message), ref _message, value);
        }

        string _callStack;
        [Size(-1)]
        public string CallStack{
            get => _callStack;
            set => SetPropertyValue(nameof(CallStack), ref _callStack, value);
        }

        string _logicalOperationStack;
        [Size(-1)]
        public string LogicalOperationStack{
            get => _logicalOperationStack;
            set => SetPropertyValue(nameof(LogicalOperationStack), ref _logicalOperationStack, value);
        }

        DateTime _dateTime;
        [ModelDefault("DisplayFormat", "{0:G}")]
        public DateTime DateTime{
            get => _dateTime;
            set => SetPropertyValue(nameof(DateTime), ref _dateTime, value);
        }

        int _processId;

        public int ProcessId{
            get => _processId;
            set => SetPropertyValue(nameof(ProcessId), ref _processId, value);
        }

        int _threadId;

        public int Thread{
            get => _threadId;
            set => SetPropertyValue(nameof(Thread), ref _threadId, value);
        }

        long _timestamp;
        [Indexed]
        public long Timestamp{
            get => _timestamp;
            set => SetPropertyValue(nameof(Timestamp), ref _timestamp, value);
        }
        [Browsable(false)]
        public IObjectSpace ObjectSpace{ get; set; }

        int _called;

        public int Called{
            get => _called;
            set => SetPropertyValue(nameof(Called), ref _called, value);
        }

        string _applicationTitle;
        [Size(255)] 
        public string ApplicationTitle{
            get => _applicationTitle;
            set => SetPropertyValue(nameof(ApplicationTitle), ref _applicationTitle, value);
        }
    }
}