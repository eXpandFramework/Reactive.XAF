using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.XtraEditors;
using DevExpress.XtraSpellChecker.Native;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.SpellChecker {
    public sealed class SpellCheckerModule : ReactiveModuleBase {
        public static ReactiveTraceSource TraceSource { get; set; }

        static SpellCheckerModule() => TraceSource = new ReactiveTraceSource(nameof(SpellCheckerModule));

        public SpellCheckerModule() {
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(SystemWindowsFormsModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            
            SpellCheckTextBoxBaseFinderManager.Default.RegisterClass(typeof(StringEdit), typeof(StringEditTextBoxFinder));
            SpellCheckTextBoxBaseFinderManager.Default.RegisterClass(typeof(LargeStringEdit), typeof(LargeStringEditTextBoxFinder));
            SpellCheckTextControllersManager.Default.RegisterClass(typeof(StringEdit), typeof(SimpleTextEditTextController));
            SpellCheckTextControllersManager.Default.RegisterClass(typeof(LargeStringEdit), typeof(SimpleTextEditTextController));
            SpellCheckTextControllersManager.Default.RegisterClass(typeof(MemoEdit), typeof(SimpleTextEditTextController));
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Connect().Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules, IModelReactiveModulesSpellChecker>();
        }
    }
    
    public class LargeStringEditTextBoxFinder : MemoEditTextBoxFinder {
        public override TextBoxBase GetTextBoxInstance(Control editControl) => editControl is LargeStringEdit edit ? base.GetTextBoxInstance(edit) : null;
    }
    public class StringEditTextBoxFinder : TextEditTextBoxFinder {
        public override TextBoxBase GetTextBoxInstance(Control editControl) => editControl is StringEdit edit ? base.GetTextBoxInstance(edit) : null;
    }
}