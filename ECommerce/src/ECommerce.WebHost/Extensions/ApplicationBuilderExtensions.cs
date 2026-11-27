using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Localization;
using ECommerce.Module.Localization;

namespace ECommerce.WebHost.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// No legacy IdentityServer middleware is invoked in JWT-only configuration.
        /// Keep this method minimal to avoid pulling in IdentityServer or cookie auth.
        /// </summary>
        public static IApplicationBuilder UseCustomizedIdentity(this IApplicationBuilder app)
        {
            // Intentionally do NOT call UseIdentityServer() or configure cookies here.
            // Authentication/authorization for APIs is handled via JwtBearer in Program.cs
            return app;
        }

        public static IApplicationBuilder UseCustomizedStaticFiles(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = (context) =>
                    {
                        var headers = context.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue
                        {
                            NoCache = true,
                            NoStore = true,
                            MaxAge = TimeSpan.FromDays(-1)
                        };
                    }
                });
            }
            else
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = (context) =>
                    {
                        var headers = context.Context.Response.GetTypedHeaders();
                        headers.CacheControl = new CacheControlHeaderValue
                        {
                            Public = true,
                            MaxAge = TimeSpan.FromDays(60)
                        };
                    }
                });
            }

            return app;
        }

        public static IApplicationBuilder UseCustomizedRequestLocalization(this IApplicationBuilder app)
        {
            string defaultCultureUI = GlobalConfiguration.DefaultCulture;

            try
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    try
                    {
                        var cultureRepo = scope.ServiceProvider.GetService<IRepositoryWithTypedId<Culture, string>>();

                        if (cultureRepo != null)
                        {
                            try
                            {
                                GlobalConfiguration.Cultures = cultureRepo.Query().ToList();
                            }
                            catch
                            {
                                GlobalConfiguration.Cultures = Array.Empty<Culture>();
                            }
                        }
                        else
                        {
                            GlobalConfiguration.Cultures = Array.Empty<Culture>();
                        }

                        var config = scope.ServiceProvider.GetService<IConfiguration>();
                        if (config != null)
                        {
                            var cfgValue = config.GetValue<string>("Global.DefaultCultureUI");
                            if (!string.IsNullOrWhiteSpace(cfgValue))
                            {
                                defaultCultureUI = cfgValue;
                            }
                        }
                    }
                    catch
                    {
                        GlobalConfiguration.Cultures = Array.Empty<Culture>();
                    }
                }
            }
            catch
            {
                GlobalConfiguration.Cultures = Array.Empty<Culture>();
            }

            var supportedCultures = GlobalConfiguration.Cultures.Select(c => c.Id).ToArray();
            if (supportedCultures.Length == 0)
            {
                supportedCultures = new[] { GlobalConfiguration.DefaultCulture };
            }

            app.UseRequestLocalization(options =>
                options
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures)
                    .SetDefaultCulture(defaultCultureUI ?? GlobalConfiguration.DefaultCulture)
                    .RequestCultureProviders.Insert(0, new EfRequestCultureProvider())
            );

            return app;
        }
    }
}
