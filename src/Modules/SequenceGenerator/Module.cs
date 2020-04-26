using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Validation;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.SequenceGenerator{
    public sealed class SequenceGeneratorModule : ReactiveModuleBase{
        [PublicAPI]
        public const string ModelCategory = "Xpand.SequenceGenerator";
        

        static SequenceGeneratorModule(){
            TraceSource=new ReactiveTraceSource(nameof(SequenceGeneratorModule));
        }

        public SequenceGeneratorModule(){
            RequiredModuleTypes.Add(typeof(ValidationModule));
        }

        [Browsable(false)][PublicAPI]
        public static Type SequenceStorageType{ get; set; } = typeof(SequenceStorage);
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public override void Setup(XafApplication application){
            base.Setup(application);
            application.Connect(SequenceStorageType)
                .TakeUntilDisposed(this)
                .Subscribe();
        }
    }
}
