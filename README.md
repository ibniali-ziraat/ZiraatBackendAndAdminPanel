ZiratApiProject
Ziraat is a modular ASP.NET Core 8 application for ecommerce, vendor management, and farmer services. It includes HTTP APIs, a Razor/AngularJS admin interface, farm and orchard workflows, payments, notifications, and weather integrations.

Project structure
text
Copy
ECommerce/
  ECommerce.sln                  Application solution
  global.json                   .NET 8 SDK selection
  src/
    ECommerce.WebHost/           Startup, settings, migrations, Swagger, themes
    ECommerce.Infrastructure/    Shared data, module, localization, web abstractions
    Modules/                    Business APIs, services, models, views, assets
    Database/                   Static and localization SQL data
  build/                        MSBuild module packaging tasks
  scripts/                      Additional database change scripts
  test/                         .NET tests and frontend/browser checks
  ECommerce.Module.WishList.Tests.Controllers/  Additional test project
  aws-beanstalk/                Deployment artifacts
The application runs as a single host. Modules are registered through WebHost project references, modules.json, and individual module.json manifests. They implement IModuleInitializer to register services and configure middleware.

Modules cover catalog, inventory, vendors, pricing, cart, checkout, orders, payments, shipping, CMS, localization, discussion, reviews, and wish lists. Farmer dashboards and farm workflows live primarily in ECommerce.Module.Core; notifications, SignalR, and Hangfire jobs have separate modules.

Requirements
.NET 8 SDK, as selected by ECommerce/global.json.
SQL Server and permission to create/update the application database.
Entity Framework CLI 8.0.0 for migrations.
Node.js and Chrome for frontend/browser checks only.
SQL Server is configured in startup and the EF design-time factory. Hangfire also uses ConnectionStrings:DefaultConnection for SQL Server storage.

Configuration
Settings are in ECommerce/src/ECommerce.WebHost/:

File	Purpose
appsettings.json	Base application and integration settings.
appsettings.Development.json	Development overrides, including a different database connection.
appsettings.docker.json	Legacy PostgreSQL settings; requires provider changes before use.
Properties/launchSettings.json	Local profiles, environment, ports, and login URL.
The host loads base and environment-specific JSON, development user secrets, environment variables, and command-line arguments through ASP.NET Core configuration. It also loads database-backed application settings through AddEntityFrameworkConfig. The database must be reachable during startup.

Use your own credentials through development user secrets or deployment environment variables. Replace : with __ in environment variable names, for example ConnectionStrings__DefaultConnection and Payments__Razorpay__KeySecret. Examples below use placeholders.

Setting / section	Purpose and keys
ConnectionStrings:DefaultConnection	SQL Server connection for EF Core and Hangfire.
Jwt	Key, Issuer, Audience for bearer tokens. Use a private signing key of at least 32 bytes.
AzureBlobStorage	ConnectionString, ContainerName, PublicEndpoint, FarmerDirectory, SupplierDirectory; SubDirectories contains Documents, Images, Profile.
Cloudinary	CloudName, ApiKey, ApiSecret, PublicEndpoint for media storage.
Authentication	Facebook AppId/AppSecret and Google ClientId/ClientSecret. Login availability depends on authentication registration.
Firebase	Enabled, ServiceAccountPath or ServiceAccountJson, AppName, AndroidChannelId for push delivery. Enabled delivery requires a service account.
Weather:UserAgent	User agent for weather requests.
Weather:Meteoblue	ApiKey, ForecastBaseUrl, ImageBaseUrl, WarningsUrl, DefaultPackage.
Weather:WeatherApi	ApiKey, ForecastUrl, AlertsUrl.
Weather:Alerts	Enabled, Provider, Schedule, MinimumSeverities. Configured schedule */10 * * * * runs every ten minutes; severities are Moderate, Severe, Extreme.
FarmWorkflow:DeliveryEnabled	Controls external farm advisory delivery.
Payments:Razorpay	KeyId, KeySecret, WebhookSecret, Currency (INR), PayoutAccountNumber, PayoutMode (IMPS), PayoutUrl.
Fast2Sms	Url, ApiKey, Route, SenderId, MessageTemplateId, Flash, OtpExpiryMinutes (10) for SMS/OTP.
SmtpServer, SmtpPort, SmtpUsername, SmtpPassword	Top-level email settings. Supplied port is 587; the sender uses STARTTLS.
Logging, Serilog	Warning-level defaults and rolling-file settings. Serilog output requires host logger integration.
For local work without external notifications, override Firebase:Enabled, Weather:Alerts:Enabled, and FarmWorkflow:DeliveryEnabled to false.

Run locally
Run these PowerShell commands from the repository root:

powershell
Copy
cd ECommerce
dotnet tool install --global dotnet-ef --version 8.0.0
dotnet restore ECommerce.sln
dotnet build ECommerce.sln

cd src/ECommerce.WebHost
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Ziraat;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet user-secrets set "Jwt:Key" "<your-private-signing-key-at-least-32-bytes>"
dotnet user-secrets set "Firebase:Enabled" "false"
dotnet user-secrets set "Weather:Alerts:Enabled" "false"
dotnet user-secrets set "FarmWorkflow:DeliveryEnabled" "false"
dotnet ef database update --context SimplDbContext
dotnet run --launch-profile ECommerce.WebHost
If dotnet-ef is already installed, ensure its version matches EF Core 8. Replace the connection string with one suitable for your SQL Server authentication. Run migrations from WebHost: the design-time factory reads configuration relative to the current directory, plus user secrets and environment variables.

The project launch profile listens on http://localhost:49208:

Path	Purpose
/admin/login	Administrator sign-in.
/admin	Administration interface.
/swagger	Interactive API documentation.
/swagger/v1/swagger.json	OpenAPI specification.
/api	Common API prefix; see Swagger for individual endpoints.
API clients use JWT bearer tokens; the admin UI uses ASP.NET Core Identity cookies. Use an administrator account provisioned in your database. HTTPS redirection is enabled; configure a local HTTPS endpoint/certificate when needed. IIS Express has separate ports in launchSettings.json.

In Visual Studio 2022 with .NET 8 support, open ECommerce/ECommerce.sln and select ECommerce.WebHost as the startup project.

Database changes
EF migrations are in src/ECommerce.WebHost/Migrations. Additional SQL scripts are in scripts/, and static/localization data is in src/Database/. Review scripts for the target deployment and existing data before applying them. Avoid SQL that duplicates an EF migration.

Tests
From ECommerce/:

powershell
Copy
dotnet test ECommerce.sln
# Focused module tests:
dotnet test test/ECommerce.Module.Core.Tests/ECommerce.Module.Core.Tests.csproj
run-tests.ps1 and run-tests.sh also run projects under test/. See the frontend README for browser prerequisites and commands. Additional UI checks are in ECommerce/test/ui/.

Deployment
From ECommerce/:

powershell
Copy
dotnet publish src/ECommerce.WebHost/ECommerce.WebHost.csproj -c Release -o artifacts/publish
Provide configuration and credentials explicitly in the target environment. The host disables default content items and marks appsettings.json as never copied to output; verify configuration, module assets, and any Firebase service-account file in the published artifact. Apply migrations before starting the application.

The Dockerfiles, docker-entrypoint.sh, simpl-build.sh, and AWS Beanstalk artifacts include older deployment setups. The main Dockerfile targets .NET 5 and references SimplCommerce.WebHost. simpl-build.sh rewrites the database provider and deletes migrations. Revise these for the current .NET 8/SQL Server application before use.
