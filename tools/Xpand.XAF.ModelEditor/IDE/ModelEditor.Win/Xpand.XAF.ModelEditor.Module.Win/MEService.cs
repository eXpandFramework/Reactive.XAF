

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Diagnostics;
using Xpand.Extensions.Reactive.Transform.System.IO;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.ModelEditor.Module.Win.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.ModelEditor.Module.Win {
    public static class MEService {
        static readonly HttpClient HttpClient = new();

        static MEService() => HttpClient.DefaultRequestHeaders.Add("User-Agent",$"{nameof(MEService)}-{Environment.MachineName}");

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => Observable.Defer(() => application.DeleteMESettings(true).ShowModelListView().RunME().ToUnit()
                    .Merge(application.CloseViewWhenNotSettings()))
		            .MergeToUnit(application.AuthenticateRequests()))
                .Merge(manager.WhenExtendingModel().Do(extenders => extenders.Add<IModelApplication,IModelApplicationME>()).ToUnit());

        private static IObservable<string> AuthenticateRequests(this XafApplication application) 
	        => application.WhenModelChanged().Select(modelApplication => ((IModelApplicationME)modelApplication).ModelEditor.GithubToken).WhenNotDefaultOrEmpty()
				.Do(token => HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token));

        private static IObservable<XafApplication> RunME(this IObservable<XafApplication> source)  
            => source.MergeIgnored(application => application.WhenViewOnFrame().WhenFrame(ViewType.ListView)
                .Select(frame => frame.GetController<ListViewProcessCurrentObjectController>())
                .SelectMany(controller => controller.WhenCustomProcessSelectedItem(true)
	                .Do(_ => {
		                controller.View.ObjectSpace.CommitChanges();
		                ((Window) controller.Frame).Close();
	                })
	                .Select(e => e.SelectedObjects.Cast<XafModel>().First())
	                .Do(model => {
		                if (model.Project.CannotRunMEMessage != null) {
			                throw new CannotRunMEException(model.Project.CannotRunMEMessage);
		                }
	                })
	                .SelectMany(xafModel => xafModel.DownloadME(((IModelApplicationME)application.Model).ModelEditor,application)
		                .StartMEProcess(xafModel))));

        internal static XafApplication DeleteMESettings(this XafApplication application,bool setup=false) {
            var path = GetMESettingsPath();
            if (File.Exists(path)) {
                File.Delete(path);
            }

            if (setup) {
                if (File.Exists(GetReadyPath())) {
                    File.Delete(GetReadyPath());
                }
            }
            return application;
        }

        internal static IObservable<string> WhenMESettings(){
	        return new FileInfo(GetMESettingsPath()).WhenCreated()
		        .ToConsole().Finally(() => {})
		        .SelectMany(info => Observable.Defer(()
			        => $"{JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(info.FullName))?.Solution}"
				        .Observe()).Retry())
		        .ObserveOnContext().Finally(() => {});
	        // return Observable.Using(() => new FileSystemWatcher(MeInstallationPath), watcher => {
			      //   watcher.EnableRaisingEvents = true;
			      //   var meSettingsPath = GetMESettingsPath();
			      //   return watcher.WhenEvent<FileSystemWatcher, FileSystemEventArgs>(nameof(watcher.Created))
				     //    .Where(t => t.args.FullPath == meSettingsPath)
				     //    .SelectMany(_ => Observable.Defer(()
					    //     => $"{JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(meSettingsPath))?.Solution}"
						   //      .Observe()).Retry())
				     //    .Merge("".Observe().Do(_ => File.CreateText(GetReadyPath())).IgnoreElements());
		       //  })
		       //  .ObserveOn(SynchronizationContext.Current!)
		       //  .TraceModelEditorWindowsFormsModule();
        }

        internal static string GetReadyPath() => $"{AppDomain.CurrentDomain.ApplicationPath()}\\Ready.txt";
        internal static string GetMESettingsPath() => $"{MeInstallationPath}\\MESettings.json";

        private static IObservable<string> DownloadME(this XafModel xafModel, IModelME modelME, XafApplication application) {
            var assemblyPath = xafModel.Project.AssemblyPath;
            if (!File.Exists(assemblyPath)) {
                throw new UserFriendlyException($"File {assemblyPath} not found, please build the project");
            }

            var versionsGroup = Directory.GetFiles(Path.GetDirectoryName(assemblyPath)!,"DevExpress.ExpressApp*.dll")
	            .Where(s => {
		            var fileName = Path.GetFileName(s);
		            return fileName.StartsWith("DevExpress.ExpressApp")&&!fileName.Contains("CodeAnalysis");
	            })
                .GroupBy(s => Version.Parse(FileVersionInfo.GetVersionInfo(s).FileVersion!)).ToArray();
            if (versionsGroup.Length > 1){
	            var conflicts = versionsGroup.SelectMany(grouping => grouping.Take(1).Select(path => (name: Path.GetFileName(path),
			            version: Version.Parse(FileVersionInfo.GetVersionInfo(path).FileVersion!)))).JoinCommaSpace();
	            throw new UserFriendlyException($"Multiple DevExpress versions found in {assemblyPath} (${conflicts})");
            }
            if (!versionsGroup.Any()) {
                throw new UserFriendlyException($"Cannot find any DevExpress assembly in {Path.GetDirectoryName(assemblyPath)}");
            }

            var dxVersion = versionsGroup.First().Key;
            if (new Uri(modelME.DownloadUrl).IsFile) {
	            return dxVersion.Observe().DownloadME(xafModel, modelME);
            }
            return dxVersion.RXXafReleaseVersion(modelME.DownloadPreRelease, modelME.NoFoundVersion())
                .Do( version => application.ShowViewStrategy.ShowMessage($"Latest release is {version} downloading..."))
	            .DownloadME(xafModel,modelME);
        }

        private static IObservable<string> DownloadME(this IObservable<Version> source, XafModel xafModel, IModelME modelME)
	        => source.SelectMany(version => {
		        var meType = "WinDesktop";
		        var directory = $"{MeInstallationPath}\\{meType}\\{xafModel.Project.TargetFramework}\\{version}\\";
		        string filename = $"Xpand.XAF.ModelEditor.{meType}.exe";
		        var path = $"{directory}{filename}";
		        if (!File.Exists(path)) {
			        if (!Directory.Exists(directory)) {
				        Directory.CreateDirectory(directory);
			        }

			        var name = $"{meType}{Regex.Match(xafModel.Project.TargetFramework,"net(\\d+)\\.\\d+").Groups[1].Value}";
			        return modelME.DownloadUrl.StringFormat($"{version}", name).Observe()
				        .TraceModelEditorWindowsFormsModule(s => $"Download {s}")
				        .SelectMany(url => MEBytes(url)
					        .ObserveOn(SynchronizationContext.Current!)
					        .Do(bytes => {
						        var pathToZip = $"{directory}Xpand.XAF.ModelEditor.{name}.zip";
						        bytes.Save(pathToZip);
						        using var archive = ZipFile.OpenRead(pathToZip);
						        archive.ExtractToDirectory(directory,true);
					        }).To(path));
		        }

		        return path.Observe();
	        });

        private static IObservable<byte[]> MEBytes(string url) {
	        var uri = new Uri(url);
	        if (uri.IsFile) {
		        url = uri.LocalPath;
	        }
	        if (File.Exists(url)) {
		        return File.ReadAllBytes(url).Observe();
	        }
	        return HttpClient.GetByteArrayAsync(url).ToObservable();
        }

        private static Func<Version, IObservable<Version>> NoFoundVersion(this IModelME modelME) 
            => dxVersion =>modelME.DevVersion==null? Observable.Throw<Version>(new VersionNotFoundException(
                $"Version {dxVersion} not on GitHub. Consider the ${nameof(IModelME.DownloadPreRelease)}, or the ({nameof(IModelME.DevVersion)}, {nameof(IModelME.DownloadUrl)}) model attributes")):modelME.DevVersion.Observe();

        private static IObservable<Version> RXXafReleaseVersion(this Version dxVersion, bool preRelease, Func<Version,IObservable<Version>> noFound) 
            => HttpClient.GetStringAsync("https://api.github.com/repos/expandframework/reactive.xaf/tags").ToObservable()
                .SelectMany(s => JsonConvert.DeserializeObject<JArray>(s)!.Select(token => Version.Parse($"{token["name"]}")))
                .Where(version => (version.Revision == 0 && !preRelease)||preRelease)
                .Where(version => $"{version.Minor}" == $"{dxVersion.Major}{dxVersion.Minor}").Take(1)
                .SwitchIfEmpty(noFound(dxVersion))
                .FirstAsync()
                .ObserveOn(SynchronizationContext.Current!)
                .TraceModelEditorWindowsFormsModule(version => $"Locating {dxVersion} release = {version}");

        public static string MeInstallationPath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Xpand.XAF.ModelEditor.Win";

        static IObservable<Unit> StartMEProcess(this IObservable<string> source, XafModel xafModel) 
            => source.SelectMany(mePath => {
                KillProcess(mePath);
                return xafModel.StartME(xafModel.Project.Path, mePath);
            }).ToUnit();

        private static IObservable<Unit> StartME(this XafModel xafModel, string fullPath, string destFileName) {
            string debugMe = xafModel.Debug ? "d" : null;
            string arguments = String.Format("{0} {4} \"{1}\" \"{3}\" \"{2}\"", debugMe, xafModel.Project.AssemblyPath,
                fullPath, xafModel.Path, xafModel.Project.IsApplicationProject);
            var process = new Process() {
	            StartInfo = new ProcessStartInfo(destFileName,arguments) {
                    WorkingDirectory = Path.GetDirectoryName(destFileName)!
	            }
            };
            Tracing.Tracer.LogSeparator("Xpand Model Editor");
            Tracing.Tracer.LogValue("MEPath",destFileName);
            Tracing.Tracer.LogValue("MEArgs",arguments);
            process.StartWithEvents();
            return process.WhenErrorDataReceived().WhenNotDefault()
	            .Do(s => throw new Exception(s)).ToUnit();
        }

        static void KillProcess(string path){
            const string wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using var searcher = new ManagementObjectSearcher(wmiQueryString);
            using var results = searcher.Get();
            var query = Process.GetProcesses()
                .Join(results.Cast<ManagementObject>(), p => p.Id, mo => (int) (uint) mo["ProcessId"],
                    (p, mo) => new{
                        Process = p,
                        Path = (string) mo["ExecutablePath"],
                        CommandLine = (string) mo["CommandLine"],
                    });
            foreach (var item in query.Where(arg => arg.Path==path)) {
                item.Process.Kill();
            }
        }
    }

    internal class CannotRunMEException : UserFriendlyException {
        public CannotRunMEException(string message):base(message) { }
    }
}