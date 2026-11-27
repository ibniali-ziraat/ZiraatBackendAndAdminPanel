using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Cms.Areas.Cms.ViewModels;
using ECommerce.Module.Cms.Models;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.Cms.Areas.Cms.Controllers
{
    [Area("Cms")]
    [ApiController]
    [Route("api/pages")]

    public class PageController : ControllerBase
    {
        private readonly IRepository<Page> _pageRepository;
        private readonly IContentLocalizationService _contentLocalizationService;

        public PageController(IRepository<Page> pageRepository, IContentLocalizationService contentLocalizationService)
        {
            _pageRepository = pageRepository;
            _contentLocalizationService = contentLocalizationService;
        }

        [HttpGet("[action]")]
        public IActionResult PageDetail(long id)
        {
            var page = _pageRepository.Query().FirstOrDefault(x => x.Id == id);

            if(page == null)
            {
                return NotFound();
            }

            var model = new PageVm
            {
                Name = _contentLocalizationService.GetLocalizedProperty(page, nameof(page.Name), page.Name),
                Body = _contentLocalizationService.GetLocalizedProperty(page, nameof(page.Body), page.Body),
                MetaTitle = page.MetaTitle,
                MetaKeywords = page.MetaKeywords,
                MetaDescription = page.MetaDescription
            };

            return Ok(model);
        }
    }
}
