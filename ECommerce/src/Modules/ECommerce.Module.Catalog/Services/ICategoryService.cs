using System.Threading.Tasks;
using System.Collections.Generic;
using ECommerce.Module.Catalog.Areas.Catalog.ViewModels;
using ECommerce.Module.Catalog.Models;

namespace ECommerce.Module.Catalog.Services
{
    public interface ICategoryService
    {
        Task<IList<CategoryListItem>> GetAll();

        Task Create(Category category);

        Task Update(Category category);

        Task Delete(Category category);
    }
}
