using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace DevExpress.XAF.Modules.Reactive.Controllers{
    public class RegisterActionsViewController:ViewController{
        static ReplaySubject<Func<RegisterActionsViewController, ActionBase[]>> _subject=CreateSubject();

        private static ReplaySubject<Func<RegisterActionsViewController, ActionBase[]>> CreateSubject(){
            return new ReplaySubject<Func<RegisterActionsViewController, ActionBase[]>>();
        }

        readonly Subject<Unit> _terminator=new Subject<Unit>();
        public RegisterActionsViewController(){
            _subject.TakeUntil(_terminator)
                .Finally(() => {})
                .Subscribe(func => func(this));
        }

        protected override void Dispose(bool disposing){
            base.Dispose(disposing);
            _terminator.OnNext(Unit.Default);
        }

        internal static void RegisterAction(Func<RegisterActionsViewController, ActionBase[]> actions){
            _subject.OnNext(actions);
        }

        public static void Reset(){
            _subject = CreateSubject();
        }
    }
}