using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace ModelEditor {
    public static class XpandModelEditor {
        public static async Task StartMEAsync() {
            if (!Process.GetProcessesByName("Xpand.XAF.ModelEditor.Win").Any()) {
                var readyFile = $"{MESettingsPath}\\Xpand.XAF.ModelEditor.Win\\Ready.txt";
                if (File.Exists(readyFile)) {
                    File.Delete(readyFile);
                }

                var readyPath = $"{Path.GetDirectoryName(readyFile)}\\Ready.txt";
                if (File.Exists(readyPath)) {
                    File.Delete(readyPath);
                    Thread.Sleep(200);
                }

                var watcher = new FileSystemWatcher(Path.GetDirectoryName(readyPath)!);
                watcher.EnableRaisingEvents = true;
                var tsc = new TaskCompletionSource<bool>();

                void CreatedHandler(object s, FileSystemEventArgs e) {
                    if (e.Name == Path.GetFileName(readyPath)) {
                        tsc.SetResult(true);
                        watcher.Created -= CreatedHandler;
                    }
                }

                watcher.Created += CreatedHandler;
                Process.Start($"{MESettingsPath}\\Xpand.XAF.ModelEditor.Win\\Xpand.XAF.ModelEditor.Win.exe");

                await tsc.Task;
            }
        }

        static string MESettingsPath
            => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Xpand.XAF.ModelEditor.Win\\";

        public static async Task ExtractMEAsync() => await Task.FromResult(ExtractME());


        private static int ExtractME() {
            if (!Directory.Exists(MESettingsPath)) {
                Directory.CreateDirectory(MESettingsPath);
            }

            var assembly = typeof(XpandModelEditor).Assembly;
            using var memoryStream = new MemoryStream();
            var resourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith(".zip"));
            var resourceStream = assembly.GetManifestResourceStream(resourceName) ??
                                 throw new InvalidOperationException("packageVersion");
            var fileName = Path.GetFileNameWithoutExtension(resourceName);
            var version = Regex.Match(fileName, @"\.[\d]*\.[\d]*\.[\d]*\.[\d]*").Value.Trim('.');
            var zipPath = $"{MESettingsPath}\\Xpand.XAF.ModelEditor.Win.{version}.zip";
            if (!File.Exists(zipPath)) {
                SaveToFile(resourceStream, zipPath);
                var meDir = $"{MESettingsPath}\\Xpand.XAF.ModelEditor.Win";
                if (Directory.Exists(meDir)) {
                    foreach (var process in Directory.GetFiles(meDir, "*.exe")
                                 .SelectMany(s => Process.GetProcessesByName(Path.GetFileNameWithoutExtension(s)))) {
                        process.Kill();
                        Thread.Sleep(300);
                    }

                    Directory.Delete(meDir, true);
                }

                Directory.CreateDirectory(meDir);
                ZipFile.ExtractToDirectory(zipPath, meDir);
                return 1;
            }

            return 0;
        }

        static void SaveToFile(Stream stream, string filePath) {
            var directory = Path.GetDirectoryName(filePath) + "";
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = File.OpenWrite(filePath);
            stream.CopyTo(fileStream);
        }

        public static void WriteSettings(string solutionFileName)
            => File.WriteAllText($"{MESettingsPath}MESettings.json",
                JsonSerializer.Serialize(new { Solution = solutionFileName }));
    }
}