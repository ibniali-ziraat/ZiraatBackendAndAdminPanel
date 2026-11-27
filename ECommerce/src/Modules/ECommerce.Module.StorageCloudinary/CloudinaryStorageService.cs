using System.Diagnostics.Contracts;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.StorageCloudinary
{
    public class CloudinaryStorageService : IStorageService
    {
        private Cloudinary _cloudinary;
        private string _cloudName;
        private string _publicEndpoint;

        public CloudinaryStorageService(IConfiguration configuration)
        {
            _cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];
            _publicEndpoint = configuration["Cloudinary:PublicEndpoint"];

            Contract.Requires(!string.IsNullOrWhiteSpace(_cloudName), "CloudName configuration is required");
            Contract.Requires(!string.IsNullOrWhiteSpace(apiKey), "ApiKey configuration is required");
            Contract.Requires(!string.IsNullOrWhiteSpace(apiSecret), "ApiSecret configuration is required");

            var account = new Account(_cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);

            if (string.IsNullOrWhiteSpace(_publicEndpoint))
            {
                _publicEndpoint = $"https://res.cloudinary.com/{_cloudName}/image/upload/";
            }
        }

        public string GetMediaUrl(string fileName)
        {
            return $"{_publicEndpoint}{fileName}";
        }

        public async Task SaveMediaAsync(Stream mediaBinaryStream, string fileName, string mimeType = null)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, mediaBinaryStream),
                PublicId = Path.GetFileNameWithoutExtension(fileName),
                Overwrite = true
            };

            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                uploadParams.Tags = $"mimetype={mimeType}";
            }

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Failed to upload file to Cloudinary: {uploadResult.Error.Message}");
            }
        }

        public async Task DeleteMediaAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Filename cannot be null, empty, or whitespace.", nameof(fileName));
            }

            var publicId = Path.GetFileNameWithoutExtension(fileName);
            var deleteParams = new DeletionParams(publicId);

            var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

            if (deleteResult.Error != null && deleteResult.Result != "ok")
            {
                // Log warning but don't throw - file may already be deleted
            }
        }
    }
}
