using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Graph;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

// ReSharper disable once CheckNamespace
namespace TestApplication.MicrosoftService{
	internal static class ShowMSAccountInfoService{
		public static SimpleAction ShowMSAccountInfo(this (AgnosticModule, Frame frame) tuple) => 
			tuple.frame.Action(nameof(ShowMSAccountInfo)).As<SimpleAction>();

		public static IObservable<Unit> ShowMSAccountInfo(this ApplicationModulesManager manager){
			manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(User));
			var registerViewSimpleAction = manager.RegisterViewSimpleAction(nameof(ShowMSAccountInfo)).ActivateInUserDetails().Publish().RefCount(); 
			return manager.WhenApplication(application => registerViewSimpleAction.WhenExecute().ShowAccountInfoView().ToUnit())
				.Merge(registerViewSimpleAction.ToUnit());
		}

		private static IObservable<User> ShowAccountInfoView(this IObservable<SimpleActionExecuteEventArgs> source) =>
			source.SelectMany(e => {
					e.ShowViewParameters.CreatedView = e.Action.Application.NewView(ViewType.DetailView, typeof(User));
					e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
					return e.Action.Application.OutlookUser().ObserveOn(SynchronizationContext.Current)
						.Do(user => e.ShowViewParameters.CreatedView.CurrentObject = user);
				});

		private static IObservable<User> OutlookUser(this XafApplication application) =>
			application.AuthorizeMS().SelectMany(client => client.Me.Request().GetAsync());
	}
}