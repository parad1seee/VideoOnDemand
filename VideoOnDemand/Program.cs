using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using NLog.Web;
using VideoOnDemand.DAL;
using VideoOnDemand.DAL.Migrations;
using System;
using System.Net;

namespace VideoOnDemand
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var appBasePath = System.IO.Directory.GetCurrentDirectory();
            NLog.GlobalDiagnosticsContext.Set("appbasepath", appBasePath);

            // NLog: setup the logger first to catch all errors
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                foreach (FileTarget target in LogManager.Configuration.AllTargets)
                {
                    target.FileName = appBasePath + "/" + ((SimpleLayout)target.FileName).OriginalText;
                }

                LogManager.ReconfigExistingLoggers();

                logger.Debug("init main");

                var host = CreateWebHostBuilder(args).Build();
                var builder = new ConfigurationBuilder()
                .SetBasePath(appBasePath)
                .AddJsonFile("appsettings.json");

                Configuration = builder.Build();
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<DataContext>();
                        DbInitializer.Initialize(context, Configuration, services);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "An error occurred while seeding the database.");
                    }
                }

                host.Run();
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                    .UseDefaultServiceProvider(options => options.ValidateScopes = false)
                    .ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 1312, listenOptions =>
                        {
                            //Change before publish
                            //listenOptions.UseHttps("", "");
                            listenOptions.UseConnectionLogging();
                        });
                        options.Listen(IPAddress.Any, 1310);
                    })
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                    })
                    .UseNLog();  // NLog: setup NLog for Dependency injection
            });
    }
}
