using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Windows.SystemActions;

namespace Xpand.XAF.Modules.Windows{
    public interface IModelReactiveModuleWindows : IModelReactiveModule{
        IModelWindows Windows{ get; }
    }

    public interface IModelWindows : IModelNode{
	    IModelNotifyIcon NotifyIcon{ get;  }
	    IModelWindowsMultiInstance MultiInstance{ get;  }
	    IModelWindowsExit Exit{ get;  }
        IModelWindowsMainFormBox Form { get; } 
        bool Startup { get; set; }
        IModelSystemActions SystemActions { get; }
    }

    public interface IModelSystemActions : IModelNode, IModelList<IModelSystemAction> {
        
    }

    public interface IModelSystemAction:IModelActionLink {
        [Editor(typeof(HotKeyEditor), typeof(UITypeEditor))]
        [Required][ReadOnly(true)]
        string HotKey { get; set; }
        IModelSystemActionViews Views { get; }
        
    }

    public interface IModelSystemActionViews:IModelNode,IModelList<IModelViewLink> { }

    [KeyProperty(nameof(ViewId))]
    public interface IModelViewLink : IModelNode {
        [Browsable(false)]
        string ViewId { get; set; }
        IModelView View { get; set; }
    }

    [DomainLogic(typeof(IModelViewLink))]
    public class ModelViewLinkLogic {
        public static IModelView Get_View(IModelViewLink link) => link.Application.Views[link.ViewId];

        public static void Set_View(IModelViewLink link, IModelView value) => link.ViewId = value.Id;
    }
    public class HotKeyVisibility:IModelIsReadOnly {
        
        public bool IsReadOnly(IModelNode node, string propertyName) => ((IModelSystemAction)node).Action == null;

        public bool IsReadOnly(IModelNode node, IModelNode childNode) {
            throw new NotImplementedException();
        }
    }
    public interface IModelNotifyIcon : IModelNode {
        [Category("Menu")][DefaultValue("Show")][Localizable(true)]
        string ShowText { get; set; }
        [Category("Menu")][DefaultValue("Exit")][Localizable(true)]
        string ExitText { get; set; }
        [Category("Menu")][DefaultValue("LogOff")][Localizable(true)]
        string LogOffText { get; set; }
        [DefaultValue(true)]
        bool ShowOnDblClick { get; set; }
        bool Enabled { get; set; }
        [Category("Menu")][DefaultValue("Hide")][Localizable(true)]
        string HideText { get; set; }
    }

    public interface IModelWindowsMultiInstance : IModelNode {
        bool Disabled { get; set; }
        [DefaultValue(true)]
        bool FocusRunning { get; set; }
        [DefaultValue("{0} is is already running")]
        [Localizable(true)]
        string NotifyMessage { get; set; }
    }

    public interface IModelExitPrompt:IModelNode {
        bool Enabled { get; set; }    
        [Localizable(true)][DefaultValue("Are you sure you want to exit {0}?")]
        string Message { get; set; }
        [Localizable(true)][DefaultValue("{0}")]
        string Title { get; set; }
    }

    public interface IModelWindowsExit:IModelNode {
        IModelExitPrompt Prompt { get; }
	    IModelOnExit OnExit { get; }
        IModelOn OnEscape { get; }
        IModelOn OnDeactivation { get; }
        [Category("Exit")]
        bool ExitAfterModelEdit { get;  }
    }

    public interface IModelOnExit:IModelNode {
        bool HideMainWindow { get; set; }
        bool MinimizeMainWindow { get; set; }
    }

    public interface IModelExitApplication:IModelNode {
        bool ExitApplication { get; set; }
    }

    public interface IModelOn: IModelExitApplication {
        bool CloseWindow { get; set; }
        bool MinimizeWindow { get; set; }

        [DefaultValue(true)]
        bool ApplyInMainWindow { get; set; }
    }
    public interface IModelWindowsMainFormBox:IModelNode {
	    [DefaultValue(true)]
	    bool MinimizeBox { get; set; }
        bool PopupWindows { get; set; }
	    [DefaultValue(true)]
	    bool MaximizeBox { get; set; }
	    [DefaultValue(true)]
	    bool ControlBox { get; set; }
        [DefaultValue(true)]
	    bool ShowInTaskbar { get; set; }
        [DefaultValue(FormBorderStyle.Sizable)]
        FormBorderStyle FormBorderStyle { get; set; }
        string Text { get; set; }
    }
    
    public static class ModelReactiveModuleOneView{
        public static IObservable<IModelWindows> WindowsModel(this IObservable<IModelReactiveModules> source) 
            => source.Select(modules => modules.WindowsModel());

        public static IModelWindows WindowsModel(this IModelReactiveModules reactiveModules) 
            => ((IModelReactiveModuleWindows) reactiveModules).Windows;
    }
}