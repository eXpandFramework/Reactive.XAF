using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using TestApplication.Module;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Person = Google.Apis.People.v1.Data.Person;


namespace TestApplication.GoogleService{

	internal static class ShowGoogleAccountInfoService{
		public static SimpleAction ShowGoogleAccountInfo(this (TestApplicationModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ShowGoogleAccountInfo)).As<SimpleAction>();

		public static IObservable<Unit> ShowGoogleAccountInfo(this ApplicationModulesManager manager){
			manager.Modules.OfType<TestApplicationModule>().First().AdditionalExportedTypes.Add(typeof(EmailAddress));
            var registerViewSimpleAction = manager.RegisterViewSimpleAction(nameof(ShowGoogleAccountInfo)).Publish().RefCount();
            var googleUser = registerViewSimpleAction.WhenActivated().Select(action => action).SelectMany(action => action.Application.GoogleUser());
            return registerViewSimpleAction.SelectMany(action => googleUser
                    .SelectMany(person => action.WhenExecute()
                        .Do(e => {
                            e.ShowViewParameters.CreatedView = e.Action.Application.NewView(ViewType.DetailView, typeof(EmailAddress));
                            e.ShowViewParameters.CreatedView.CurrentObject = person.EmailAddresses.First();
                        }))).ToUnit()
				.Merge(registerViewSimpleAction.ActivateInUserDetails().ToUnit())
                .Merge(manager.ConfigureModel());
		}
		private static IObservable<Unit> ConfigureModel(this ApplicationModulesManager manager) 
			=> manager.WhenGeneratingModelNodes(modelApplication => modelApplication.BOModel)
				.Do(model => model.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().OAuth
					.AddScopes(PeopleService.Scope.UserinfoEmail,PeopleService.Scope.UserinfoProfile)).ToUnit();

		private static IObservable<Person> GoogleUser(this XafApplication application) 
            => application.AuthorizeGoogle().NewService<PeopleService>()
                .SelectMany(service => {
                    var request = service.People.Get("people/me");
                    request.RequestMaskIncludeField = "person.emailAddresses";
                    return request.ExecuteAsync();
                });
	}
}