using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.StringExtensions;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;

namespace ALL.Tests{
    public abstract class BaseTest:Xpand.TestsLib.BaseTest{
        protected async Task EasyTest<TAppAdapter>(Func<TAppAdapter> appAdapter,Func<TAppAdapter,string,TestApplication> applicationFactory,Func<ICommandAdapter,Task> executeCommands,string connectionString=null) where TAppAdapter:IApplicationAdapter{
            connectionString ??= InMemoryDataStoreProvider.ConnectionString;
            using (var winAdapter = appAdapter()){
                var testApplication = applicationFactory(winAdapter, connectionString);
                try{
                    var commandAdapter = winAdapter.CreateCommandAdapter();
                    commandAdapter.Execute(new LoginCommand());
                    await executeCommands(commandAdapter);
                }
                finally{
                    var easyTestSettingsFile = testApplication.EasyTestSettingsFile();
                    if (File.Exists(easyTestSettingsFile)){
                        File.Delete(easyTestSettingsFile);
                    }
                    WriteLine($"Finally EasyTest {TestContext.CurrentContext.Result.Outcome.Status}");
                    winAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);
                    if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed){
                        var path = Path.GetDirectoryName(easyTestSettingsFile);
                        WriteLine(path);
                        foreach (var file in Directory.GetFiles(path!, "*.log")){
                            var zipPPath = $"{Path.GetDirectoryName(file)}\\{Path.GetFileNameWithoutExtension(file)}.zip";
                            using (var gZipStream = new GZipStream(File.Create(zipPPath), CompressionMode.Compress)){
                                var bytes = File.ReadAllText(file).Bytes();
                                gZipStream.Write(bytes,0,bytes.Length);
                            }
                            WriteLine($"Attaching {zipPPath}");
                            TestContext.AddTestAttachment(zipPPath);
                        }
                    }
                }
            }
        }

    }
}