using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Search.Models;

namespace ECommerce.Module.Search.Areas.Search.Controllers
{
    [Area("Search")]
    [ApiController]
    [Authorize(Roles = "admin")]
    [Route("api/search")]
    public class SearchApiController : ControllerBase
    {
        private readonly IRepository<Query> _queryRepository;

        public SearchApiController(IRepository<Query> queryRepository)
        {
            _queryRepository = queryRepository;
        }

        [HttpGet("most-search-keywords")]
        public IActionResult MostSearchKeywords()
        {
            var model = _queryRepository.Query()
                .GroupBy(x => x.QueryText)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Keyword = g.Key, Count = g.Count() })
                .Take(5);

            return new JsonResult(model);
        }
    }
}
