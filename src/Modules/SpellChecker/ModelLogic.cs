using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.SpellChecker{
	public interface IModelReactiveModulesSpellChecker : IModelReactiveModule{
		IModelSpellChecker SpellChecker{ get; }
		
	}
	
	public enum FilePathResolutionMode {
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		None,
		Absolute,
		RelativeToApplicationFolder,
	}

	public static class SpellCheckerModelExtensions {
		public static IModelSpellChecker SpellChecker(this IModelApplication application) 
			=> application.ToReactiveModule<IModelReactiveModulesSpellChecker>().SpellChecker;
	}

	public interface IModelSpellChecker : IModelNode{
		[DXDescription("DevExpress.ExpressApp.SpellChecker.IModelSpellChecker,Enabled")]
		[Category("Behavior")]
		[DefaultValue(true)]
		bool Enabled { get; set; }
		[DXDescription("DevExpress.ExpressApp.SpellChecker.IModelSpellChecker,AlphabetPath")]
		[Category("Data")]
		[Localizable(true)]
		[DefaultValue("Dictionaries\\EnglishAlphabet.txt")]
		string AlphabetPath { get; set; }
		[DXDescription("DevExpress.ExpressApp.SpellChecker.IModelSpellChecker,GrammarPath")]
		[Category("Data")]
		[DefaultValue("Dictionaries\\English.aff")]
		[Localizable(true)]
		string GrammarPath { get; set; }
		[DXDescription("DevExpress.ExpressApp.SpellChecker.IModelSpellChecker,DefaultDictionaryPath")]
		[Category("Data")]
		[DefaultValue("Dictionaries\\American.xlg")]
		[Localizable(true)]
		string DefaultDictionaryPath { get; set; }
		[DXDescription("DevExpress.ExpressApp.SpellChecker.IModelSpellChecker,CustomDictionaryPath")]
		[Category("Data")]
		[DefaultValue("Dictionaries\\Custom.txt")]
		[Localizable(true)]
		string CustomDictionaryPath { get; set; }
		[DXDescription("DevExpress.ExpressApp.SpellChecker.IModelSpellChecker,DefaultDictionaryPathResolution")]
		[Category("Behavior")]
		[DefaultValue(FilePathResolutionMode.RelativeToApplicationFolder)]
		FilePathResolutionMode PathResolutionMode { get; set; }

	}

	[DomainLogic(typeof(IModelSpellChecker))]
	public static class ModelStoreToDiskLogic {
		public static string Get_Folder(IModelSpellChecker modelSpellChecker) 
			=> $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\{modelSpellChecker.Application.Title}\\{nameof(IModelReactiveModulesSpellChecker.SpellChecker)}";	}
	


}