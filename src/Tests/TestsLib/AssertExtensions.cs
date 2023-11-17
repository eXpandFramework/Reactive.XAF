using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.XtraLayout;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib {
    public static class AssertExtensions {
        public static IObservable<TabbedControlGroup> AssertTabbedGroup(this XafApplication application,
            Type objectType = null, int tabPagesCount = 0,Func<DetailView,bool> match=null,Func<IModelTabbedGroup, bool> tabMatch=null,[CallerMemberName]string caller="")
            => application.AssertTabControl<TabbedControlGroup>(objectType,match,tabMatch,caller)
                .If(group => tabPagesCount > 0 && group.TabPages.Count != tabPagesCount,group => group.Observe().DelayOnContext()
                        .SelectMany(_ => new Exception(
                            $"{nameof(AssertTabbedGroup)} {objectType?.Name} expected {tabPagesCount} but was {group.TabPages}").ThrowTestException(caller).To<TabbedControlGroup>()),
                    group => group.Observe())
                .Merge(application.WhenTabControl<TabbedControlGroup>(objectType,match)).Replay().AutoConnect();
    }
}