using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoOnDemand.Common.Utilities;
using VideoOnDemand.DAL;
using VideoOnDemand.DAL.Abstract;
using VideoOnDemand.DAL.Repository;
using VideoOnDemand.DAL.UnitOfWork;
using VideoOnDemand.ScheduledTasks;
using VideoOnDemand.Services.Interfaces;
using VideoOnDemand.Services.Interfaces.Exporting;
using VideoOnDemand.Services.Interfaces.External;
using VideoOnDemand.Services.Jobs;
using VideoOnDemand.Services.Services;
using VideoOnDemand.Services.Services.Exporting;
using VideoOnDemand.Services.Services.External;
using VideoOnDemand.Services.StartApp;
using System;

namespace VideoOnDemand.SchedulerExecutable
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"{env.ContentRootPath}/../VideoOnDemand/appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("Connection"));
                //options.UseLazyLoadingProxies(); Lazy loading
                options.EnableSensitiveDataLogging(false);
            });

            #region Register services

            #region Basis services

            services.AddScoped<IDataContext>(provider => provider.GetService<DataContext>());
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<HashUtility>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJWTService, JWTService>();
            services.AddTransient<IAccountService, AccountService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddTransient<IImageService, ImageService>();
            services.AddTransient<IS3Service, S3Service>();

            #endregion

            #region Notification services

            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISMSService, SMSService>();

            services.AddScoped<ITwillioService, TwillioService>();
            services.AddScoped<IFCMService, FCMService>();
            services.AddScoped<ISESService, SESService>();

            #endregion

            #region Payment services

            services.AddScoped<IBraintreeService, BraintreeService>();
            services.AddScoped<IStripeService, StripeService>();

            #endregion

            var config = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfileConfiguration());
            });

            services.AddSingleton(config.CreateMapper());

            #endregion

            // Scheduled tasks
            services.AddSingleton<IScheduledTask, OneWeekAfterRegistration>();
            services.AddSingleton<IScheduledTask, EverydayUpdate>();

            services.AddScheduler((sender, args) =>
            {
                Console.Write(args.Exception.Message);
                args.SetObserved();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
