using System;
using System.Text.Encodings.Web;
using System.Linq;
using System.Text.Unicode;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.WebEncoders;
using Microsoft.OpenApi.Models;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Modules;
using ECommerce.Infrastructure.Web;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;
using ECommerce.Module.Localization.Extensions;
using ECommerce.WebHost.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// CONFIGURE SERVICES
// ---------------------------------------------------------------------------
ConfigureServices();
var app = builder.Build();

// ---------------------------------------------------------------------------
// CONFIGURE PIPELINE
// ---------------------------------------------------------------------------
Configure(app, builder.Environment);
app.Run();


void ConfigureServices()
{
    var configuration = builder.Configuration;
    var environment = builder.Environment;

    // -----------------------------------------------
    // Database config
    // -----------------------------------------------
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    configuration.AddEntityFrameworkConfig(options => options.UseSqlServer(connectionString));

    GlobalConfiguration.WebRootPath = environment.WebRootPath;
    GlobalConfiguration.ContentRootPath = environment.ContentRootPath;

    // -----------------------------------------------
    // Identity cookies are used by the admin UI; API callers still receive JSON 401/403.
    // -----------------------------------------------
    builder.Services.AddIdentity<User, Role>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredLength = 4;
    })
    .AddEntityFrameworkStores<SimplDbContext>()
    .AddSignInManager<AccountSignInManager>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/access-denied";
        options.Events.OnValidatePrincipal = async context =>
        {
            await SecurityStampValidator.ValidatePrincipalAsync(context);
            if (context.Principal == null)
            {
                return;
            }

            var userAccess = context.HttpContext.RequestServices.GetRequiredService<IUserAccessService>();
            if (!await userAccess.CanAccessAsync(context.Principal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            }
        };
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new { error = "Unauthorized", message = "Authentication is required to access this resource." });
                return context.Response.WriteAsync(result);
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new { error = "Forbidden", message = "You do not have permission to access this resource." });
                return context.Response.WriteAsync(result);
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

    // -----------------------------------------------
    // Core modules
    // -----------------------------------------------
    builder.Services.AddModules();
    builder.Services.AddCustomizedDataStore(configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();
    builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddTransient(typeof(IRepositoryWithTypedId<,>), typeof(RepositoryWithTypedId<,>));
    builder.Services.AddScoped<SlugRouteValueTransformer>();

    // Register DbConnectionOpenLogger for DI (for DB connection logging)
    builder.Services.AddSingleton<ECommerce.WebHost.Diagnostics.DbConnectionOpenLogger>();

    // -----------------------------------------------
    // Localization
    // -----------------------------------------------
    builder.Services.AddMemoryCache();
    builder.Services.AddCustomizedLocalization();

    // -----------------------------------------------
    // Controllers and Razor views. Existing API routes remain attribute-routed.
    // -----------------------------------------------
    builder.Services.AddCustomizedMvc(GlobalConfiguration.Modules);
    builder.Services.Configure<RazorViewEngineOptions>(
        options => { options.ViewLocationExpanders.Add(new ThemeableViewLocationExpander()); });
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.Configure<WebEncoderOptions>(options =>
    {
        options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
    });

    //For SMS settings
    builder.Services.Configure<Fast2SmsSettings>(builder.Configuration.GetSection("Fast2Sms"));


    builder.Services.ConfigureModules();
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // -----------------------------------------------
    // CORS
    // -----------------------------------------------
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // -----------------------------------------------
    // JWT for mobile/API bearer clients, Identity cookie for admin browser UI.
    // -----------------------------------------------
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "BearerOrAdminCookie";
        options.DefaultChallengeScheme = "BearerOrAdminCookie";
        options.DefaultScheme = "BearerOrAdminCookie";
    })
    .AddPolicyScheme("BearerOrAdminCookie", "Bearer or admin cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            var hasSignalRAccessToken = context.Request.Path.StartsWithSegments("/signalr", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(context.Request.Query["access_token"]);

            return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || hasSignalRAccessToken
                ? JwtBearerDefaults.AuthenticationScheme
                : IdentityConstants.ApplicationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // dev
        options.SaveToken = true;

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"] ?? "ECommerceAPI",

            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"] ?? "ECommerceUsers",

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "SuperSecretDevelopmentKey12345!")
            ),

            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userAccess = context.HttpContext.RequestServices.GetRequiredService<IUserAccessService>();
                if (!await userAccess.CanAccessAsync(context.Principal))
                {
                    context.Fail("Account is no longer available.");
                }
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/signalr", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // -----------------------------------------------
    // Swagger
    // -----------------------------------------------
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ECommerce API",
            Version = "v1"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {token}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

static bool IsApiRequest(HttpRequest request)
{
    return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        || request.Headers.Accept.Any(x => x?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        || request.Headers["X-Requested-With"] == "XMLHttpRequest";
}



// ---------------------------------------------------------------------------
// CONFIGURE PIPELINE
// ---------------------------------------------------------------------------
void Configure(WebApplication app, IWebHostEnvironment env)
{
    app.UseHttpsRedirection();
    app.UseCustomizedStaticFiles(env);

    app.UseRouting();

    app.UseCors("AllowAll");

    // Bearer JWT for mobile/API calls, admin cookie for browser UI calls.
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseCustomizedRequestLocalization();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommerce API V1");
        c.RoutePrefix = "swagger";
    });

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    });

    var moduleInitializers = app.Services.GetServices<IModuleInitializer>();
    foreach (var moduleInitializer in moduleInitializers)
    {
        moduleInitializer.Configure(app, env);
    }

    Console.WriteLine("? ECommerce started with Identity + JWT-only API authentication (no redirects).");
}
