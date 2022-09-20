using System;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.XtraSpellChecker;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.SpellChecker{
    public static class SpellCheckerService{
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(ViewType.DetailView)
                .Where(frame => frame.SpellCheckerModel().Enabled)
                .SelectUntilViewClosed(frame => frame.View.ToDetailView().GetItems<PropertyEditor>().ToNowObservable()
                .Where(editor => editor.MemberInfo.FindAttributes<SpellCheckAttribute>().Any())
                .SelectMany(editor => editor.WhenControlCreated().Select(item => item.Control).StartWith(editor.Control).WhenNotDefault().Cast<Control>()
                        .SelectMany(CreateSpellCheckerComponent)))
                    
            ).ToUnit();

        private static IModelSpellChecker SpellCheckerModel(this Frame frame) => frame.Application.Model.SpellChecker();
        static readonly SpellCheckerISpellDictionary EnglishDictionary = (SpellCheckerISpellDictionary)typeof(DictionaryHelper).Method("CreateEnglishDictionary",Flags.StaticAnyVisibility).Call(null);
        static IObservable<DevExpress.XtraSpellChecker.SpellChecker> CreateSpellCheckerComponent(this Control parentContainer) 
            => Observable.Using(() => new DevExpress.XtraSpellChecker.SpellChecker(), spellChecker => {
                spellChecker.Dictionaries.Add(EnglishDictionary);
                spellChecker.ParentContainer = parentContainer;
                spellChecker.SpellCheckMode = SpellCheckMode.AsYouType;
                var typeOptions = spellChecker.CheckAsYouTypeOptions;
                typeOptions.ShowSpellCheckForm = true;
                typeOptions.CheckControlsInParentContainer = true;
                spellChecker.Culture = CultureInfo.CurrentUICulture;
                spellChecker.CheckContainer();
                return spellChecker.ReturnObservable().DoNotComplete();
            });

        internal static IObservable<TSource> TraceSpellChecker<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, SpellCheckerModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

    }
}