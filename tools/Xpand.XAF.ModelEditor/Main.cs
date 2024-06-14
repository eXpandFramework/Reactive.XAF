using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Win.Core;
using DevExpress.ExpressApp.Win.Core.ModelEditor;
using DevExpress.ExpressApp.Win.Templates.ActionControls.Binding;
using DevExpress.Persistent.Base;

// ReSharper disable once CheckNamespace
namespace Xpand.XAF.ModelEditor {
    public static class MainClass {
        private static ModelEditorForm _modelEditorForm;
        private static void HandleException(Exception e) {
            MessageBox.Show(e.ToString());
            Tracing.Tracer.LogError(e);
            Messaging.GetMessaging(null).Show(ModelEditorForm.Title, e);
        }
        private static void OnException(object sender, ThreadExceptionEventArgs e) {
            HandleException(e.Exception);
        }
        static void CheckAssemblyFile(PathInfo pathInfo) {
            if (!File.Exists(pathInfo.AssemblyPath)) {
                pathInfo.AssemblyPath = Path.Combine(Environment.CurrentDirectory, pathInfo.AssemblyPath);
                if (!File.Exists(pathInfo.AssemblyPath)) {
                    throw new Exception($"The file '{pathInfo.AssemblyPath}' couldn't be found.");
                }
            }
        }

        [STAThread]
        public static void Main(string[] args){
            
            var iconName = typeof(MainClass).Assembly.GetManifestResourceNames().First(s => s.EndsWith("ExpressApp.ico"));
            var manifestResourceStream = typeof(MainClass).Assembly.GetManifestResourceStream(iconName);
            var icon = new Icon(manifestResourceStream ?? throw new InvalidOperationException() );
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            
            Application.ThreadException += OnException;
            SplashScreen splashScreen = null;
            try {
                var strings = args;
                if (args.Length>4&&args[0]=="d"){
                    MessageBox.Show(
                        $"Attach to {Path.GetFileName(AppDomain.CurrentDomain.SetupInformation.ApplicationBase)}");
                    strings = args.Skip(1).ToArray();
                }

                DesignerOnlyCalculator.IsRunFromDesigner = true;
                ModuleHelper.IsDesignMode = true;
                splashScreen = new SplashScreen();
                splashScreen.Start();
                var pathInfo = new PathInfo(strings);
                Tracing.Tracer.LogSeparator("PathInfo");
                Tracing.Tracer.LogText(pathInfo.ToString());
                Tracing.Tracer.LogSeparator("PathInfo");
                CheckAssemblyFile(pathInfo);
                var modelControllerBuilder = new ModelControllerBuilder();
                var settingsStorageOnRegistry = new SettingsStorageOnRegistry(@"Software\Developer Express\eXpressApp Framework\Model Editor");
                var modelEditorViewController = modelControllerBuilder.GetController(pathInfo);

                WinSimpleActionBinding.Register();
                WinSingleChoiceActionBinding.Register();
                WinParametrizedActionBinding.Register();
                PopupWindowShowActionBinding.Register();
                _modelEditorForm = new ModelEditorForm(modelEditorViewController, settingsStorageOnRegistry);
                _modelEditorForm.Shown += (_, _) => splashScreen?.Stop();
                _modelEditorForm.Disposed += (sender, _) => ((IModelEditorSettings)sender)?.ModelEditorSaveSettings();
                _modelEditorForm.SetCaption($"{Path.GetFileNameWithoutExtension(pathInfo.AssemblyPath)}/{Path.GetFileName(pathInfo.LocalPath)}");
                _modelEditorForm.Icon=icon;
                
                Application.Run(_modelEditorForm);
            } catch (Exception exception) {
                HandleException(exception);
            }
            finally {
                splashScreen?.Stop();
            }

        }

    }
}
