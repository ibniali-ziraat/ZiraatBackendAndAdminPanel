using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Search.Areas.Search.ViewModels;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;


namespace ECommerce.Module.Module.Search;
    public interface IVendorSearchService
    {
        Task<VendorSearchResult> SearchProductsByVendorAsync(VendorSearchOption option);
    }

