using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PureIP.Portal.Domain.Models.Security;
using PureIP.Portal.Services.Security;

namespace PureIP.Portal.Customer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(Controllers.ActivityFilter));
                options.ModelMetadataDetailsProviders.Add(new HumanizerMetadataProvider());
            });

            services.Configure<Domain.Models.CustomerPortalSettings>(Configuration.GetSection("AppSettings:CustomerPortalSettings"));

            Data.ServiceConfigurator.SetDatabaseConfiguration(services,
                Configuration.GetConnectionString("PortalDatabase"),
                Configuration.GetConnectionString("CDRDatabase"),
                Configuration.GetConnectionString("CustomerDatabase"),
                Configuration.GetConnectionString("NumberDatabase"));
            Data.ServiceConfigurator.SetDependency(services);
            Services.ServiceConfigurator.SetDependency(services);

            //identity
            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<Data.Security.SecurityContext>()
                .AddDefaultTokenProviders();
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizePermissions.Quote);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, 
            Domain.Data.ILoggingRepository loggingRepositor,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
            Domain.Data.IServiceConfigurator serviceConfigurator)
        {
            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            loggerFactory.AddProvider(new Services.Logging.LoggerProvider((cat, loglevel) => loglevel >= LogLevel.Warning, loggingRepositor, httpContextAccessor));

            app.UseMvc(routes =>
            {
                routes.MapAreaRoute("admin_route", "Admin", "Admin/{controller}/{action=Index}/{id?}");
                routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");
            });

            serviceConfigurator.Migrate();
        }

        public class HumanizerMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider
        {
            public void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context)
            {
                var modelMetadata = context.DisplayMetadata;
                var propertyAttributes = context.Attributes;
                var propertyName = context.Key.Name;

                if (IsTransformRequired(propertyName, modelMetadata, propertyAttributes))
                {
                    if (propertyName.EndsWith("Id") && propertyName.Length > 2)
                        propertyName = propertyName.Substring(0, propertyName.Length - 2);
                    propertyName = string.Concat(propertyName.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
                    modelMetadata.DisplayName = () => propertyName;
                }
            }

            private static bool IsTransformRequired(string propertyName, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadata modelMetadata, IReadOnlyList<object> propertyAttributes)
            {
                if (!string.IsNullOrEmpty(modelMetadata.SimpleDisplayProperty))
                    return false;

                if (propertyAttributes.OfType<System.ComponentModel.DisplayNameAttribute>().Any())
                    return false;

                if (propertyAttributes.OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().Any())
                    return false;

                if (string.IsNullOrEmpty(propertyName))
                    return false;

                return true;
            }
        }
    }
}
