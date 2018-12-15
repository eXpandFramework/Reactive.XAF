using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace DevExpress.XAF.Modules.Reactive.Controllers{
    public class RegisterActionsWindowController:WindowController{
        public static IObservable<Frame> WhenFrameAssigned => FrameAssignedSubject.TakeUntil(Terminator);
        static ReplaySubject<Func<RegisterActionsWindowController, ActionBase[]>> _subject=CreateSubject();

        private static ReplaySubject<Func<RegisterActionsWindowController, ActionBase[]>> CreateSubject(){
            return new ReplaySubject<Func<RegisterActionsWindowController, ActionBase[]>>();
        }

        private static readonly Subject<Frame> FrameAssignedSubject=new Subject<Frame>();
        static readonly Subject<Unit> Terminator=new Subject<Unit>();
        public RegisterActionsWindowController(){
            _subject.TakeUntil(Terminator).Subscribe(func => func(this));
            
        }

        protected override void Dispose(bool disposing){
            base.Dispose(disposing);
            Terminator.OnNext(Unit.Default);
        }

        internal static void RegisterAction(Func<RegisterActionsWindowController, ActionBase[]> actions){
            _subject.OnNext(actions);
        }

        protected override void OnFrameAssigned(){
            base.OnFrameAssigned();
            FrameAssignedSubject.OnNext(Frame);
        }

        public static void Reset(){
            _subject = CreateSubject();
        }
    }
}