using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Transform.System.Diagnostics;
using Xpand.TestsLib.Attributes;

namespace Xpand.Extensions.Tests{
    public class ProcessTests{
        private static string CreateScriptFile(){
            var path = $"{Path.GetTempPath()}\\Output.ps1";
            if (File.Exists(path)){
                File.Delete(path);
            }

            return path;
        }

        [Test][XpandTest()]
        public async Task WhenOutputDataReceived(){
            var path = CreateScriptFile();
            File.WriteAllLines(path,new[]{"Write-Host 'Hello'"});
            var process = new Process(){StartInfo = new ProcessStartInfo(){
                FileName = "powershell.exe",Arguments = path
            }};
            
            process.StartWithEvents();
            
            await process.WhenOutputDataReceived().FirstAsync(s => s=="Hello");
            
        }
        
        [Test][XpandTest()]
        public async Task WhenErrorDataReceived(){
            var path = CreateScriptFile();
            File.WriteAllLines(path,new[]{"Write-Error 'Fail'"});
            var process = new Process(){StartInfo = new ProcessStartInfo(){
                FileName = "powershell.exe",Arguments = path
            }};
            
            process.StartWithEvents();
            
            await process.WhenErrorDataReceived().FirstAsync(s => s.Contains("Fail"));
            
        }

        [Test][XpandTest()]
        public async Task WhenExited(){
            var path = CreateScriptFile();
            var process = new Process(){StartInfo = new ProcessStartInfo(){
                FileName = "powershell.exe",Arguments = path
            }};
            process.StartWithEvents();
            
            await process.WhenExited().FirstAsync();
            
        }

    }
}