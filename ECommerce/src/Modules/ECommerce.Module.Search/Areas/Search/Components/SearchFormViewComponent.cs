using System.Linq;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Catalog.Models;
using ECommerce.Module.Search.Areas.Search.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ECommerce.Module.Search.Areas.Search.Components
{
    public class SearchFormViewComponent : ViewComponent
    {
        private readonly IRepository<Category> _categoryRepository;

        public SearchFormViewComponent(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public IViewComponentResult Invoke()
        {
            var request = ViewContext.HttpContext.Request;
            var model = new SearchForm
            {
                Query = request.Query["query"],
                Category = request.Query["category"],
                AvailableCategories = _categoryRepository.Query()
                    .Where(x => x.IsPublished && !x.IsDeleted)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Slug
                    })
                    .ToList()
            };

            return View("~/Areas/Search/Views/Shared/Components/SearchForm/Default.cshtml", model);
        }
    }
}
