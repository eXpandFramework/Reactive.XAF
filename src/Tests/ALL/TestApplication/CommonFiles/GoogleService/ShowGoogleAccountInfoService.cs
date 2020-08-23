using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Person = Google.Apis.People.v1.Data.Person;

// ReSharper disable once CheckNamespace
namespace TestApplication.GoogleService{

	internal static class ShowGoogleAccountInfoService{
		public static SimpleAction ShowGoogleAccountInfo(this (AgnosticModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ShowGoogleAccountInfo)).As<SimpleAction>();

		public static IObservable<Unit> ShowGoogleAccountInfo(this ApplicationModulesManager manager){
			manager.Modules.OfType<AgnosticModule>().First().AdditionalExportedTypes.Add(typeof(EmailAddress));
            var registerViewSimpleAction = manager.RegisterViewSimpleAction(nameof(ShowGoogleAccountInfo)).ActivateInUserDetails().Publish().RefCount(); 
			return manager.WhenApplication(application => registerViewSimpleAction.WhenExecute().ShowAccountInfoView().ToUnit())
				.Merge(registerViewSimpleAction.ToUnit())
				.Merge(manager.ConfigureModel());
		}
		private static IObservable<Unit> ConfigureModel(this ApplicationModulesManager manager) 
			=> manager.WhenGeneratingModelNodes(modelApplication => modelApplication.BOModel)
				.Do(model => model.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().OAuth
					.AddScopes(PeopleService.Scope.UserinfoEmail,PeopleService.Scope.UserinfoProfile,"https://www.googleapis.com/auth/calendar.events")).ToUnit();
		private static IObservable<Person> ShowAccountInfoView(this IObservable<SimpleActionExecuteEventArgs> source) 
            => source.SelectMany(e => {
					e.ShowViewParameters.CreatedView = e.Action.Application.NewView(ViewType.DetailView, typeof(EmailAddress));
                    e.ShowViewParameters.TargetWindow = TargetWindow.NewWindow;
					return e.Action.Application.GoogleUser().ObserveOn(SynchronizationContext.Current)
						.Do(user => e.ShowViewParameters.CreatedView.CurrentObject = user.EmailAddresses.First());
				});

		private static IObservable<Person> GoogleUser(this XafApplication application) 
            => application.AuthorizeGoogle().NewService<PeopleService>()
                .SelectMany(service => {
                    var request = service.People.Get("people/me");
                    request.RequestMaskIncludeField = "person.emailAddresses";
                    return request.ExecuteAsync();
                });
	}
}