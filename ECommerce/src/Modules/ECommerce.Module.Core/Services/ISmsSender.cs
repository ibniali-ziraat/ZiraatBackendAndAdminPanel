using System.Threading.Tasks;

namespace ECommerce.Module.Core.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}