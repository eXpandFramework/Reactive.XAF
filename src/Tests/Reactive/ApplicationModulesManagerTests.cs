using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
	public class ApplicationModulesManagerTests:ReactiveCommonTest{
		[Test][XpandTest()]
		public void WhenCustomizeTypeInfo(){
			using (var application = NewXafApplication()){
				application.WhenApplicationModulesManager().WhenCustomizeTypesInfo()
					.Do(_ => (_.e.TypesInfo.FindTypeInfo(typeof(R))).CreateMember(nameof(WhenCustomizeTypeInfo),
						typeof(string))).Test();

				DefaultReactiveModule(application);

				application.TypesInfo.FindTypeInfo(typeof(R)).FindMember(nameof(WhenCustomizeTypeInfo)).ShouldNotBeNull();

			}

		}
		[Test][XpandTest()]
		public void WhenExtendingModel(){
			using (var application = NewXafApplication()){
				application.WhenApplicationModulesManager().WhenExtendingModel()
					.Do(extenders => extenders.Add<IModelApplication, IModelStaticText>())
					.Test();
				
				DefaultReactiveModule(application);
				
				application.Model.GetType().GetInterfaces().ShouldContain(typeof(IModelStaticText));

			}

		}
		[Test][XpandTest()]
		public void WhenGeneratingModelNodes_FromType(){
			using (var application = NewXafApplication()){
				application.WhenApplicationModulesManager().WhenGeneratingModelNodes<IModelImageSources>()
					.Do(imageSources => imageSources.AddNode<IModelFileImageSource>(nameof(WhenGeneratingModelNodes_FromType)))
					.Test();
				
				DefaultReactiveModule(application);

				application.Model.ImageSources.FirstOrDefault(source =>
					source.Id() == nameof(WhenGeneratingModelNodes_FromType)).ShouldNotBeNull();

			}

		}
		[Test][XpandTest()]
		public void WhenGeneratingModelNodes_FromExpression(){
			using (var application = NewXafApplication()){
				application.WhenApplicationModulesManager().WhenGeneratingModelNodes(modelApplication => modelApplication.ImageSources)
					.Do(imageSources => imageSources.AddNode<IModelFileImageSource>(nameof(WhenGeneratingModelNodes_FromType)))
					.Test();
				
				DefaultReactiveModule(application);

				application.Model.ImageSources.FirstOrDefault(source =>
					source.Id() == nameof(WhenGeneratingModelNodes_FromType)).ShouldNotBeNull();

			}

		}

	}
}