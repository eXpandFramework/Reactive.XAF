using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestApplication.Blazor.Server
{
    public class Program
    {
        private static bool ContainsArgument(string[] args, string argument) {
            return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
        }
        public static int Main(string[] args) 
        {
            if(ContainsArgument(args, "help") || ContainsArgument(args, "h")) 
            {
                Console.WriteLine("Updates the database when its version does not match the application's version.");
                Console.WriteLine();
                Console.WriteLine($"    {Assembly.GetExecutingAssembly().GetName().Name}.exe --updateDatabase [--forceUpdate --silent]");
                Console.WriteLine();
                Console.WriteLine("--forceUpdate - Marks that the database must be updated whether its version matches the application's version or not.");
                Console.WriteLine("--silent - Marks that database update proceeds automatically and does not require any interaction with the user.");
                Console.WriteLine();
                Console.WriteLine($"Exit codes: 0 - {DBUpdater.StatusUpdateCompleted}");
                Console.WriteLine($"            1 - {DBUpdater.StatusUpdateError}");
                Console.WriteLine($"            2 - {DBUpdater.StatusUpdateNotNeeded}");
            }
            else 
            {
                DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
                IHost host = CreateHostBuilder(args).Build();
                if(ContainsArgument(args, "updateDatabase")) 
                {
                    using(var serviceScope = host.Services.CreateScope()) 
                    {
                        return serviceScope.ServiceProvider.GetRequiredService<IDBUpdater>().Update(ContainsArgument(args, "forceUpdate"), ContainsArgument(args, "silent"));
                    }
                }
                else {
                    host.Run();
                }
            }
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
