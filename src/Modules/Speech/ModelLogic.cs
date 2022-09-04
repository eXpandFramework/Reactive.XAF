using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech{
    public interface IModelReactiveModuleSpeech : IModelReactiveModule{
        IModelSpeech Speech{ get; }
    }

    public interface IModelSpeech : IModelNode{
        [CriteriaOptions("TypeInfo")]
        [Editor("DevExpress.ExpressApp.Win.Core.ModelEditor.CriteriaModelEditorControl, DevExpress.ExpressApp.Win" + XafAssemblyInfo.VersionSuffix + XafAssemblyInfo.AssemblyNamePostfix, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        string DefaultAccountCriteria { get; set; }
        [Required]
        string DefaultStorageFolder { get; set; }
        [Category("SSML")]
        [DefaultValue(@"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
    {0}
</speak>")]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        string SSMLSpeakFormat { get; set; }
        [Category("SSML")]
        [DefaultValue(@"<voice name=""{0}"">
        {1}
    </voice>")]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        string SSMLVoiceFormat { get; set; }
        
        [Browsable(false)]
        ITypeInfo TypeInfo { get; }
    }
    
    [DomainLogic(typeof(IModelSpeech))]
    public class ModelSpeechLogic{
        public static ITypeInfo Get_TypeInfo(IModelSpeech link) => link.Application.BOModel.GetClass(typeof(SpeechAccount)).TypeInfo;
        public static string Get_DefaultStorageFolder(IModelSpeech link) 
            => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{link.Application.Title}\\{nameof(SpeechModule)}\\StorageFolder";
    }


    
}