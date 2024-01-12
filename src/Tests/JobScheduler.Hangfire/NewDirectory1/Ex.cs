using System;
using System.Data;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Windows;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.NewDirectory1 {
    static class Ex {
        // public static IObservable<Unit> StartTest<TStartup>(this IObservable<IHostBuilder> source, string url,
        //     string contentRoot, string user, Func<BlazorApplication, IObservable<Unit>> test,
        //     Action<IServiceCollection> configure = null, string browser = null,WindowPosition inactiveWindowBrowserPosition=WindowPosition.None,
        //     LogContext logContext=default,WindowPosition inactiveWindowLogContextPosition=WindowPosition.None)
        //     where TStartup : class  
        //     => source.SelectMany(builder => builder.StartTest<TStartup>( url, contentRoot, user, test,
        //         configure, browser, inactiveWindowBrowserPosition, logContext, inactiveWindowLogContextPosition));

        // public static IObservable<Unit> StartTest<TStartup>(this IHostBuilder builder, string url,
        //     string contentRoot, string user, Func<BlazorApplication, IObservable<Unit>> test, Action<IServiceCollection> configure = null, string browser = null,
        //     WindowPosition inactiveWindowBrowserPosition = WindowPosition.None,LogContext logContext=default,WindowPosition inactiveWindowLogContextPosition=WindowPosition.None) where TStartup : class  
        //     => builder.ConfigureWebHostDefaults<TStartup>( url, contentRoot,configure).Build()
        //         .Observe().SelectMany(host => XafApplicationMonitor.Application.EnsureMultiTenantMainDatabase()
        //             .DeleteModelDiffs(application => application.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString"),user).Cast<BlazorApplication>()
        //             .TakeUntil(host.Services.WhenApplicationStopping())
        //             .SelectMany(application => application.WhenLoggedOn(user).IgnoreElements()
        //                 .Merge(application.WhenMainWindowCreated().To(application))
        //                 .TakeUntilDisposed(application).Cast<BlazorApplication>()
        //                 .SelectMany(xafApplication => test(xafApplication).BufferUntilCompleted().To(xafApplication)))
        //             .Select(application => application.ServiceProvider)
        //             .DoAlways(() => host.Services.StopTest()).Take(1)
        //             .MergeToUnit(host.Run(url, browser,inactiveWindowBrowserPosition)))
        //         .LogError()
        //         .Log(logContext,inactiveWindowLogContextPosition,true);
        
        // private static IObservable<Unit> Run(this IHost host,string url, string browser,WindowPosition inactiveWindowPosition=WindowPosition.None) 
        //     => host.Services.WhenApplicationStopping().Publish(whenHostStop => whenHostStop
        //         .Merge(host.Services.WhenApplicationStarted().SelectMany(_ => new Uri(url).Start(browser)
        //                 .Do(process => process.MoveToMonitor(inactiveWindowPosition))
        //                 .SelectMany(process => whenHostStop.Do(_ => AppDomain.CurrentDomain.KillAll(process.ProcessName))))
        //             .MergeToUnit(Observable.Start(() => host.RunAsync().ToObservable().Select(unit => unit)).Merge())));
        
        
        // public static (Guid id, string connectionString) GetTenant(this SqlConnection connection, string user){
        //     var query = "SELECT ID, ConnectionString FROM Tenant WHERE Name = @Name";
        //     using var command = new SqlCommand(query, connection);
        //     command.Parameters.AddWithValue("@Name", user.Split('@')[1]);
        //     if (connection.State!=ConnectionState.Open) connection.Open();
        //     using var reader = command.ExecuteReader();
        //     return reader.Read() ? (reader.GetGuid(0), reader.GetString(1))
        //         : throw new InvalidOperationException("Tenant not found.");
        // }
        
        // public static IObservable<XafApplication> DeleteModelDiffs(this IObservable<XafApplication> source,Func<XafApplication,string> connectionStringSelector,string user=null)  
        //     => source.Do(application => {
        //         application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
        //         var tenantProvider = application.GetService<ITenantProvider>();
        //         var connectionString = connectionStringSelector(application);
        //         if (tenantProvider == null){
        //             application.DeleteModelDiffs(connectionString);
        //         }
        //         else{
        //             if (!new SqlConnectionStringBuilder(connectionString).DBExist()) return;
        //             using var sqlConnection = new SqlConnection(connectionString);
        //             var tenant = sqlConnection.GetTenant(user);
        //             tenantProvider.TenantId = tenant.id;
        //             application.DeleteModelDiffs(tenant.connectionString);
        //         }
        //     });
        
        
        // public static bool DbExist(this XafApplication application,string connectionString=null) {
        //     var builder = new SqlConnectionStringBuilder(connectionString??application.ConnectionString);
        //     var initialCatalog = "Initial catalog";
        //     var databaseName = builder[initialCatalog].ToString();
        //     builder.Remove(initialCatalog);
        //     using var sqlConnection = new SqlConnection(builder.ConnectionString);
        //     return sqlConnection.DbExists(databaseName);
        // }
        // public static bool DbExists(this IDbConnection dbConnection, string databaseName=null){
        //     if (dbConnection.State != ConnectionState.Open) {
        //         dbConnection.Open();
        //     }
        //     using var dbCommand = dbConnection.CreateCommand();
        //     dbCommand.CommandText = $"SELECT db_id('{databaseName??dbConnection.Database}')";
        //     return dbCommand.ExecuteScalar() != DBNull.Value;
        // }
        
        // public static bool DBExist(this SqlConnectionStringBuilder builder) {
        //     var initialCatalog = builder.InitialCatalog;
        //     builder.Remove("Initial catalog");
        //     using var sqlConnection = new SqlConnection(builder.ConnectionString);
        //     return sqlConnection.DbExists(initialCatalog);
        // }
        // public static void DeleteModelDiffs(this XafApplication application,string connectionString=null) {
        //     connectionString ??= application.ConnectionString;
        //     if (!application.DbExist(connectionString)) return;
        //     using var sqlConnection = new SqlConnection(connectionString);
        //     sqlConnection.Open();
        //     using var sqlCommand = sqlConnection.CreateCommand();
        //     // sqlCommand.CommandText=new []{typeof(ModelDifference),typeof(ModelDifferenceAspect)}
        //     //     .SelectMany(type => application.GetRequiredService<TDBContext>().Model.FindEntityTypes(type)
        //     //         .Select(entityType => entityType.GetTableName()))
        //     //     .Select(table => $"IF OBJECT_ID('{table}', 'U') IS NOT NULL Delete FROM {table};").StringJoin("");
        //     // throw new NotImplementedException();
        //     // sqlCommand.ExecuteNonQuery();
        // }
        
        // public static IObservable<BlazorApplication> EnsureMultiTenantMainDatabase(this IObservable<BlazorApplication> source){
        //     var subscribed = new BehaviorSubject<bool>(false);
        //     return source.SelectMany(application => {
        //         if (application.GetService<ITenantProvider>() == null){
        //             return application.Observe();
        //         }
        //         if (application.DbExist(application.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString"))){
        //             return application.WhenMainWindowCreated().DoNotComplete()
        //                 .TakeUntil(subscribed.WhenDefault())
        //                 .SelectMany(window => window.GetController<LogoffController>().LogoffAction.Trigger().To(application))
        //                 .TakeUntil(application.WhenDisposed().Do(_ => subscribed.OnNext(false))).Take(1)
        //                 .Merge(subscribed.WhenDefault().To(application).WhenDefault(blazorApplication => blazorApplication.IsDisposed()));
        //         }
        //         subscribed.OnNext(true);
        //         return application.WhenLoggedOn("Admin").IgnoreElements().To<BlazorApplication>();
        //     });
        // }

        
        // private static IHostBuilder ConfigureWebHostDefaults<TStartup>(this IHostBuilder builder,string url, string contentRoot,Action<IServiceCollection> configure=null) where TStartup : class 
        //     => builder.ConfigureWebHostDefaults(webBuilder => {
        //         webBuilder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, contentRoot)));
        //         
        //         webBuilder.UseUrls(url);
        //         webBuilder.UseStartup(context => typeof(TStartup).CreateInstance(context.Configuration));
        //         webBuilder.ConfigureServices(services => {
        //             services.AddTransient<XafApplicationMonitor>();
        //             services.Decorate<IXafApplicationFactory, XafApplicationMonitor>();
        //             configure?.Invoke(services);
        //         });
        //         webBuilder.ConfigureAppConfiguration((context, _) =>
        //             context.HostingEnvironment.ApplicationName = (typeof(TStartup).BaseType??typeof(TStartup)).Assembly.GetName().Name!);
        //     });

        // private static void StopTest(this IServiceProvider serviceProvider){
        //     serviceProvider.StopApplication();
        //     Logger.Exit();
        // }

        // public class XafApplicationMonitor(IXafApplicationFactory innerFactory) : IXafApplicationFactory {
        //     private static readonly ISubject<BlazorApplication> ApplicationSubject = new Subject<BlazorApplication>();
        //     public static IObservable<BlazorApplication> Application => ApplicationSubject.AsObservable();
        //     public BlazorApplication CreateApplication() => ApplicationSubject.PushNext(innerFactory.CreateApplication());
        // }

    }
}