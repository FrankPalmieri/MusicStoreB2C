using MusicStoreB2C.App_Start;
using MusicStoreB2C.Components;
using MusicStoreB2C.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace MusicStoreB2C
{
    public class Startup
    {
        private readonly Platform _platform;
        private readonly ILogger _logger;
        public static ActiveDirB2C adB2C;
        public static string TaskServiceUrl;

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
            // Set up configuration sources.
            // Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1'
            // is found in both the registered sources, then the later source will win. By this way a Local config
            // can be overridden by a different setting while deployed remotely.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("config.json")
                .AddJsonFile("config.local.json") // contains local settings not shared back to git, etc.
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
		        .AddJsonFile("hosting.json", optional: true)
                //All environment variables in the process's context flow in as configuration values.
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            _platform = new Platform();

            adB2C = new ActiveDirB2C(loggerFactory)
            {
                OnResetPasswordRequested = B2CResetPasswordRequested,
                OnCanceledSignIn = B2CCanceledSignIn,
                OnOtherFailure = OtherFailure
            };

        }

        public IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add EF services to the services container
            if (_platform.UseInMemoryStore)
            {
                services.AddDbContext<MusicStoreContext>(options =>
                    options.UseInMemoryDatabase());
            }
            else
            {
                services.AddDbContext<MusicStoreContext>(options =>
                    options.UseSqlServer(Configuration[StoreConfig.ConnectionStringKey.Replace("__", ":")]));
            }

            services.AddDbContext<TodoContext>(opt => opt.UseInMemoryDatabase());
            
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    // TODO: Fix this for our site
                    builder.WithOrigins("http://example.com");
                });
            });


            services.AddLogging();

            services.AddRouting(options => 
            {
                options.LowercaseUrls = true;
            });

            // Add MVC services to the services container.
            services.AddMvc().AddXmlSerializerFormatters();

            // Add Authentication services.
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Add memory cache services
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            // Add session related services.
            services.AddSession();

            // Add the system clock service
            services.AddSingleton<Components.ISystemClock, Components.SystemClock>();

            // Configure Auth
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "ManageStore",
                    authBuilder =>
                    {
                        authBuilder.RequireClaim("Group", "MusicStoreAdmin");
                    });
                //options.AddPolicy(
                //    "ManageStore",
                //    authBuilder =>
                //    {
                //        authBuilder.RequireClaim("ManageStore", "Allowed");
                //    });
            });

            services.AddSingleton<ITodoRepository, TodoRepository>();

            // http://ardalis.com/how-to-list-all-services-available-to-an-asp-net-core-app
            _logger.LogDebug($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                _logger.LogDebug($"Service: {service.ServiceType.FullName}\n      Lifetime: {service.Lifetime}\n      Instance: {service.ImplementationType?.FullName}");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Add the console logger.
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            TaskServiceUrl = Configuration["api:TaskServiceUrl"];

            // StatusCode pages to gracefully handle status codes 400-599.
            app.UseStatusCodePagesWithRedirects("~/Home/StatusCodePage");
            
            // Configure error handling middleware. UseDeveloperExceptionPage to show 
            app.UseDeveloperExceptionPage();
            app.UseExceptionHandler("/Home/Error");

            app.UseDatabaseErrorPage();
            
            // Configure Session.
            app.UseSession();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Configure Azure AD B2C
            adB2C.Configure(app, env, Configuration);

            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Configure MVC routes
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areaRoute",
                    template: "{area:exists}/{controller}/{action}",
                    defaults: new { action = "Index" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "api",
                    template: "{controller}/{id?}");
            });

            //Populates the MusicStore sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
        }

#if blah
        // Used for avoiding yellow-screen-of-death TODO 
        private Task AuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        { 
            notification.HandleResponse(); 

            if (notification.ProtocolMessage.ErrorDescription != null && notification.ProtocolMessage.ErrorDescription.Contains("AADB2C90118")) 
            { 
                // If the user clicked the reset password link, redirect to the reset password route 
                notification.Response.Redirect("/Account/ResetPassword"); 
            } 
            else if (notification.Exception.Message == "access_denied") 
            { 
                // If the user canceled the sign in, redirect back to the home page 
                notification.Response.Redirect("/"); 
            } 
            else 
            { 
                notification.Response.Redirect("/Home/Error?message=" + notification.Exception.Message); 
            } 

 
            return Task.FromResult(0); 
        } 

        private Task OnSecurityTokenValidated(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        { 
            // If you wanted to keep some local state in the app (like a db of signed up users), 
            // you could use this notification to create the user record if it does not already 
            // exist. 

            return Task.FromResult(0); 
        } 
#endif

        private Task B2CResetPasswordRequested(FailureContext context)
        {
            context.Response.Redirect("/Account/ResetPassword");
            return Task.FromResult(0);
        }

        private Task B2CCanceledSignIn(FailureContext context)
        {
            var encoder = System.Text.Encodings.Web.HtmlEncoder.Default;
            context.Response.Redirect("/Home/Error?message=" + encoder.Encode(context.Failure.Message));
            return Task.FromResult(0);
        }
        private Task OtherFailure(FailureContext context)
        {
            var encoder = System.Text.Encodings.Web.HtmlEncoder.Default;
            context.Response.Redirect("/Home/Error?message=" + encoder.Encode(context.Failure.Message));
            return Task.FromResult(0);
        }
    }
}
