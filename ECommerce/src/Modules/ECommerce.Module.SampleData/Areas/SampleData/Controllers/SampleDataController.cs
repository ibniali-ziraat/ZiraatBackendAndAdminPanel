using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Module.SampleData.Areas.SampleData.ViewModels;
using ECommerce.Module.SampleData.Services;

namespace ECommerce.Module.SampleData.Areas.SampleData.Controllers
{
    [Area("SampleData")]
    [ApiController]
    [Route("api/sampleData")]
    public class SampleDataController :ControllerBase
    {
        private readonly ISampleDataService _sampleDataService;

        public SampleDataController(ISampleDataService sampleDataService)
        {
            _sampleDataService = sampleDataService;
        }

        

        [HttpPost("[action]")]
        public async Task<IActionResult> ResetToSample(SampleDataOption model)
        {
            await _sampleDataService.ResetToSampleData(model);
            return Redirect("~/");
        }
    }
}
