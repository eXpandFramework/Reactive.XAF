using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.EasyTest.BlazorAdapter;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Windows;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.TestsLib.Blazor {
	public static class Extensions {
		public static IObservable<Unit> StartTest<TStartup>(this IObservable<IHostBuilder> source, string url,
            string contentRoot, string user, Func<BlazorApplication, IObservable<Unit>> test,Func<BlazorApplication,IObservable<Unit>> beforeSetup=null,
            Action<IServiceCollection> configureServices = null,Action<IWebHostBuilder> configureWebHostBuilder = null,Func<WebHostBuilderContext, TStartup> startupFactory=null,
            string browser = null,WindowPosition inactiveWindowBrowserPosition=WindowPosition.None,
            LogContext logContext=default,WindowPosition inactiveWindowLogContextPosition=WindowPosition.None) where TStartup : class  
            => source.SelectMany(builder => builder.StartTest( url, contentRoot, user, test,beforeSetup,
                configureServices,configureWebHostBuilder,startupFactory, browser, inactiveWindowBrowserPosition, logContext, inactiveWindowLogContextPosition));

        static IObservable<Unit> StartTest<TStartup>(this IHostBuilder builder, string url,
            string contentRoot, string user, Func<BlazorApplication, IObservable<Unit>> test,
            Func<BlazorApplication, IObservable<Unit>> beforeSetup = null,
            Action<IServiceCollection> configureServices = null,Action<IWebHostBuilder> configureWebHostBuilder = null,Func<WebHostBuilderContext, TStartup> startupFactory=null, string browser = null,
            WindowPosition inactiveWindowBrowserPosition = WindowPosition.None, LogContext logContext = default,
            WindowPosition inactiveWindowLogContextPosition = WindowPosition.None) where TStartup : class
            => builder.ConfigureWebHostDefaults(url, contentRoot, configureServices,configureWebHostBuilder,startupFactory).Build()
                .Observe().SelectMany(host => XafApplicationMonitor.Application.StartTest(host,user,beforeSetup,test)
                    .MergeToUnit(host.Run(url, browser, inactiveWindowBrowserPosition)))
                .LogError()
                // .Log(logContext, inactiveWindowLogContextPosition, true)
            ;

            static IObservable<Unit> StartTest(this IObservable<BlazorApplication> source, IHost host,
                string user, Func<BlazorApplication, IObservable<Unit>> beforeSetup,Func<BlazorApplication, IObservable<Unit>> test)
                => source.DoOnFirst(application =>application.DeleteAllData())
                    .MergeIgnored(application => beforeSetup?.Invoke(application)
                        .TakeUntil(host.Services.WhenApplicationStopping())?? Observable.Empty<Unit>())
                    .EnsureMultiTenantMainDatabase().DeleteModelDiffs(user)
                    .TakeUntil(host.Services.WhenApplicationStopping())
                    .MergeIgnored(application => application.WhenLoggedOn(user).TakeUntil(host.Services.WhenApplicationStopping()))
                    .SelectMany(application => test(application).TakeUntil(host.Services.WhenApplicationStopping()).BufferUntilCompleted().WhenNotEmpty().To(application))
                    .Catch<BlazorApplication,Exception>(_ => host.Services.StopTest().To<BlazorApplication>())
                    .Take(1)
                    .ConcatToUnit(host.Services.StopTest());

        private static IObservable<Unit> Run(this IHost host,string url, string browser,WindowPosition inactiveWindowPosition=WindowPosition.None) 
            => host.Services.WhenApplicationStopping().Select(unit => unit).Publish(whenHostStop => whenHostStop
                .Merge(host.Services.WhenApplicationStarted().SelectMany(_ => new Uri(url).Start(browser)
                        .Do(process => process.MoveToMonitor(inactiveWindowPosition))
                        .SelectMany(process => whenHostStop.DoSafe(_ => AppDomain.CurrentDomain.KillAll(process.ProcessName))))
                    .MergeToUnit(Observable.FromAsync(() => host.RunAsync()))));

        
        public static (Guid id, string connectionString) GetTenant(this SqlConnection connection, string user){
            var query = "SELECT ID, ConnectionString FROM Tenant WHERE Name = @Name";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Name", user.Split('@')[1]);
            if (connection.State!=ConnectionState.Open) connection.Open();
            using var reader = command.ExecuteReader();
            return reader.Read() ? (reader.GetGuid(0), reader.GetString(1))
                : throw new InvalidOperationException("Tenant not found.");
        }

        public static IObservable<XafApplication> DeleteData(this IObservable<XafApplication> source) {
            return source.Do(application => application.DeleteAllData());
        }  
        public static IObservable<TApp> DeleteModelDiffs<TApp>(this IObservable<TApp> source,string user=null,Func<XafApplication,string> connectionStringSelector=null) where TApp:XafApplication  
            => source.Do(application => {
                application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
                var tenantProvider = application.GetService<ITenantProvider>();
                var connectionString = connectionStringSelector?.Invoke(application)??application.GetRequiredService<IConfiguration>()
                    .GetConnectionString("ConnectionString");
                if (tenantProvider == null){
                    application.DeleteModelDiffs(connectionString);
                }
                else{
                    if (!new SqlConnectionStringBuilder(connectionString).DBExist()) return;
                    using var sqlConnection = new SqlConnection(connectionString);
                    var tenant = sqlConnection.GetTenant(user);
                    tenantProvider.TenantId = tenant.id;
                    application.DeleteModelDiffs(tenant.connectionString);
                }
            });

        
        public static bool DbExist(this XafApplication application,string connectionString=null) {
            var builder = new SqlConnectionStringBuilder(connectionString??application.ConnectionString);
            var initialCatalog = "Initial catalog";
            var databaseName = builder[initialCatalog].ToString();
            builder.Remove(initialCatalog);
            using var sqlConnection = new SqlConnection(builder.ConnectionString);
            return sqlConnection.DbExists(databaseName);
        }
        public static bool DbExists(this IDbConnection dbConnection, string databaseName=null){
            if (dbConnection.State != ConnectionState.Open) {
                dbConnection.Open();
            }
            using var dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = $"SELECT db_id('{databaseName??dbConnection.Database}')";
            return dbCommand.ExecuteScalar() != DBNull.Value;
        }

        public static bool DBExist(this SqlConnectionStringBuilder builder) {
            var initialCatalog = builder.InitialCatalog;
            builder.Remove("Initial catalog");
            using var sqlConnection = new SqlConnection(builder.ConnectionString);
            return sqlConnection.DbExists(initialCatalog);
        }
        public static void DeleteModelDiffs(this XafApplication application,string connectionString=null) {
            connectionString ??= application.ConnectionString;
            if (!application.DbExist(connectionString)) return;
            using var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            using var sqlCommand = sqlConnection.CreateCommand();
            // sqlCommand.CommandText=new []{typeof(ModelDifference),typeof(ModelDifferenceAspect)}
            //     .SelectMany(type => application.GetRequiredService<TDBContext>().Model.FindEntityTypes(type)
            //         .Select(entityType => entityType.GetTableName()))
            //     .Select(table => $"IF OBJECT_ID('{table}', 'U') IS NOT NULL Delete FROM {table};").StringJoin("");
            // throw new NotImplementedException();
            // sqlCommand.ExecuteNonQuery();
        }

        public static IObservable<BlazorApplication> EnsureMultiTenantMainDatabase(this IObservable<BlazorApplication> source){
            var subscribed = new BehaviorSubject<bool>(false);
            return source.SelectMany(application => {
                if (application.GetService<ITenantProvider>() == null){
                    return application.Observe();
                }
                if (application.DbExist(application.GetRequiredService<IConfiguration>().GetConnectionString("ConnectionString"))){
                    return application.WhenMainWindowCreated().DoNotComplete()
                        .TakeUntil(subscribed.WhenDefault())
                        .SelectMany(window => window.GetController<LogoffController>().LogoffAction.Trigger().To(application))
                        .TakeUntil(application.WhenDisposed().Do(_ => subscribed.OnNext(false))).Take(1)
                        .Merge(subscribed.WhenDefault().To(application).WhenDefault(blazorApplication => blazorApplication.IsDisposed()));
                }
                subscribed.OnNext(true);
                return application.WhenLoggedOn("Admin").IgnoreElements().To<BlazorApplication>();
            });
        }


        private static IHostBuilder ConfigureWebHostDefaults<TStartup>(this IHostBuilder builder, string url,
            string contentRoot, Action<IServiceCollection> configureServices = null,
            Action<IWebHostBuilder> configureWebHostBuilder = null,Func<WebHostBuilderContext, TStartup> startupFactory=null) where TStartup : class 
            => builder.ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, contentRoot)));
                webBuilder.UseUrls(url);
                webBuilder.UseStartup(context =>startupFactory?.Invoke(context)?? typeof(TStartup).CreateInstance(context.Configuration));
                webBuilder.ConfigureServices(services => {
                    services.AddTransient<XafApplicationMonitor>();
                    services.Decorate<IXafApplicationFactory, XafApplicationMonitor>();
                    configureServices?.Invoke(services);
                });
                
                webBuilder.ConfigureAppConfiguration((context, _) =>
                    context.HostingEnvironment.ApplicationName = (typeof(TStartup).BaseType??typeof(TStartup)).Assembly.GetName().Name!);
                configureWebHostBuilder?.Invoke(webBuilder);
            });

        private static IObservable<Unit> StopTest(this IServiceProvider serviceProvider)
            => serviceProvider.WhenApplicationStopped()
                .Zip(Observable.Defer(() => {
                    if (!(bool)serviceProvider.GetFieldValue("_disposed")) {
                        serviceProvider.StopApplication();
                        Common.Logger.Exit();
                    }
                    return Unit.Default.Observe();
                })).Take(1).ToUnit();

        public class XafApplicationMonitor(IXafApplicationFactory innerFactory) : IXafApplicationFactory {
            private static readonly ISubject<BlazorApplication> ApplicationSubject = Subject.Synchronize(new Subject<BlazorApplication>());
            public static IObservable<BlazorApplication> Application => ApplicationSubject.AsObservable();
            public BlazorApplication CreateApplication() => ApplicationSubject.PushNext(innerFactory.CreateApplication());
        }

        public static IObservable<T> SelectObject<T>(this ListView view, params T[] objects) where T : class{
            var viewEditor = (view.Editor as DxGridListEditor);
            if (viewEditor == null)
                throw new NotImplementedException(nameof(ListView.Editor));
            viewEditor.UnselectObjects(viewEditor.GetSelectedObjects());
            return objects.ToNowObservable()
                .Do(obj => viewEditor.SelectObject(obj));
        }
		
		public static IWebDriver Driver(this ICommandAdapter adapter)
			=> ((CommandAdapter)adapter).Driver;
	}
}