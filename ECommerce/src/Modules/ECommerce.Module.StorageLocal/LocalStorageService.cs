using System;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Infrastructure;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.StorageLocal
{
    public class LocalStorageService : IStorageService
    {
        private const string MediaRootFoler = "user-content";

        public string GetMediaUrl(string fileName)
        {
            return $"/{MediaRootFoler}/{fileName}";
        }

        public async Task SaveMediaAsync(Stream mediaBinaryStream, string fileName, string mimeType = null)
        {
            var dirPath = Path.Combine(GlobalConfiguration.WebRootPath, MediaRootFoler);            
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            var filePath = Path.Combine(dirPath, fileName);
            using (var output = new FileStream(filePath, FileMode.Create))
            {
                await mediaBinaryStream.CopyToAsync(output);
            }
        }

        public async Task DeleteMediaAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Filename cannot be null, empty, or whitespace.", nameof(fileName));
            }

            var filePath = Path.Combine(GlobalConfiguration.WebRootPath, MediaRootFoler, fileName);
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
    }
}
