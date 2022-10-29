using System;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.StoreToDisk{
	public interface IModelReactiveModulesStoreToDisk : IModelReactiveModule{
		IModelStoreToDisk StoreToDisk{ get; }
	}

	public static class TenantManagerModelExtensions {
		public static IModelStoreToDisk StoreToDisk(this IModelApplication application) 
			=> application.ToReactiveModule<IModelReactiveModulesStoreToDisk>().StoreToDisk;
	}

	public interface IModelStoreToDisk : IModelNode{
		[Required]
		string Folder { get; set; }
		
		bool DailyBackup { get; set; }
	}

	[DomainLogic(typeof(IModelStoreToDisk))]
	public static class ModelStoreToDiskLogic {
		public static string Get_Folder(IModelStoreToDisk modelStoreToDisk) 
			=> $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\{modelStoreToDisk.Application.Title}\\{nameof(IModelReactiveModulesStoreToDisk.StoreToDisk)}";	}
	


}