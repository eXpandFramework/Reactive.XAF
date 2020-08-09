using System;
using System.IO;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.AppDomainExtensions;
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
                    WriteLine($"Finally {TestContext.CurrentContext.Result.Outcome.Status}");
                    winAdapter.KillApplication(testApplication, KillApplicationContext.TestNormalEnded);
                    if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed){
                        var path = $@"{AppDomain.CurrentDomain.ApplicationPath()}\..\TestWinApplication\";
                        WriteLine(path);
                        foreach (var file in Directory.GetFiles(path, "*.log")){
                            WriteLine($"Attaching {file}");
                            TestContext.AddTestAttachment(file);
                        }
                    }
                }
            }
        }

    }
}