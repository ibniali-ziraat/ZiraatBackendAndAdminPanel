using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Modules;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Core
{
    public class ModuleInitializer : IModuleInitializer
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<FarmWorkflowService>();
            serviceCollection.AddScoped<FarmOperationService>();
            serviceCollection.AddScoped<FarmLabService>();
            serviceCollection.AddScoped<FarmExpansionService>();
            serviceCollection.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
            serviceCollection.AddTransient<IEntityService, EntityService>();
            serviceCollection.AddTransient<IMediaService, MediaService>();
            serviceCollection.AddTransient<IThemeService, ThemeService>();
            serviceCollection.AddTransient<IWidgetInstanceService, WidgetInstanceService>();
            serviceCollection.AddScoped<IWorkContext, WorkContext>();
            serviceCollection.AddScoped<IUserService, UserService>();
            serviceCollection.AddScoped<IUserAccessService, UserAccessService>();
            serviceCollection.AddScoped<ISmsSender, SmsSender>();
            serviceCollection.AddSingleton<SettingDefinitionProvider>();
            serviceCollection.AddScoped<ISettingService, SettingService>();
            serviceCollection.AddScoped<ICurrencyService, CurrencyService>();
            serviceCollection.AddTransient<IProfileService, ProfileService>();
            serviceCollection.AddTransient<IAddressService, AddressService>();
            serviceCollection.AddTransient<IDemographicsService, DemographicsService>();
            serviceCollection.AddTransient<ILandholdingService, LandholdingService>();
            serviceCollection.AddTransient<IOtpVerificationService, OtpVerificationService>();
            serviceCollection.AddTransient<IProfessionService, ProfessionService>();
            serviceCollection.AddTransient<IEducationService, EducationService>();
            serviceCollection.AddScoped<IBusinessInformationService, BusinessInformationService>();
            serviceCollection.AddTransient<ILicensingComplianceService, LicensingComplianceService>();
            serviceCollection.AddTransient<IProductPortfolioService, ProductPortfolioService>();
            serviceCollection.AddTransient<IProductInputTypeService, ProductInputTypeService>();
            serviceCollection.AddTransient<IProductMajorBrandService, ProductMajorBrandService>();
            serviceCollection.AddTransient<IProductCustomInputService, ProductCustomInputService>();
            serviceCollection.AddTransient<IOperationalDetailsService, OperationalDetailsService>();
            serviceCollection.AddTransient<IServiceAreaService, ServiceAreaService>();
            serviceCollection.AddTransient<IFinancialPaymentService, FinancialPaymentService>();
            serviceCollection.AddTransient<IBankDetailsService, BankDetailsService>();
            serviceCollection.AddTransient<ISupportServicesService, SupportServicesService>();
            serviceCollection.AddTransient<IVerificationRatingsService, VerificationRatingsService>();
            serviceCollection.AddTransient<ICustomerReviewService, CustomerReviewService>();
            serviceCollection.AddScoped<IAzureBlobService, AzureBlobService>();

            // Orchard Profile Services
            serviceCollection.AddScoped<IOrchardProfileService, OrchardProfileService>();
            serviceCollection.AddScoped<IOrchardVarietyService, OrchardVarietyService>();
            serviceCollection.AddScoped<IOrchardSoilAndWaterService, OrchardSoilAndWaterService>();
            serviceCollection.AddScoped<IOrchardCropNutritionService, OrchardCropNutritionService>();
            serviceCollection.AddScoped<IOrchardPlantHealthService, OrchardPlantHealthService>();
            serviceCollection.AddScoped<IOrchardInfrastructureService, OrchardInfrastructureService>();
            serviceCollection.AddScoped<IOrchardProductionService, OrchardProductionService>();
            serviceCollection.AddScoped<IOrchardInputCostService, OrchardInputCostService>();
            serviceCollection.AddScoped<IOrchardOptionalFieldsService, OrchardOptionalFieldsService>();

            serviceCollection.AddScoped<IBlobCleanupService, BlobCleanupService>();
            serviceCollection.AddScoped<IVendorProductService, VendorProductService>();
            serviceCollection.AddScoped<IMultiVendorProductStrategy, MultiVendorProductStrategy>();
            serviceCollection.AddScoped<IVendorCustomerAssociationService, VendorCustomerAssociationService>();
            serviceCollection.AddScoped<IVendorDashboardService, VendorDashboardService>();
            serviceCollection.AddScoped<IFarmerDashboardService, FarmerDashboardService>();
            serviceCollection.AddScoped<IAdminDashboardService, AdminDashboardService>();
            serviceCollection.AddScoped<IWeatherService, WeatherService>();
            GlobalConfiguration.RegisterAngularModule("simplAdmin.core");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
