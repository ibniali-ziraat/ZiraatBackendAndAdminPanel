using System.Threading.Tasks;
using ECommerce.Module.SampleData.Areas.SampleData.ViewModels;

namespace ECommerce.Module.SampleData.Services
{
    public interface ISampleDataService
    {
        Task ResetToSampleData(SampleDataOption model);
    }
}
