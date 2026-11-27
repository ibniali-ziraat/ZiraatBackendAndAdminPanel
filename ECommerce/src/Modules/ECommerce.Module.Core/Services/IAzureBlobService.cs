using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using ECommerce.Module.Core.Areas.Core.ViewModels;

public interface IAzureBlobService
{
    Task<FileUploadResponse> UploadFileAsync(FileUploadRequest request);

    /// <summary>
    /// Delete a blob file from Azure Blob Storage by blob name.
    /// Safe operation - returns true even if blob doesn't exist.
    /// </summary>
    Task<bool> DeleteFileAsync(string blobName);

    /// <summary>
    /// Delete multiple blob files from Azure Blob Storage.
    /// Safe operation - continues even if some blobs don't exist.
    /// </summary>
    Task<int> DeleteFilesAsync(params string[] blobNames);
}

public class AzureBlobService : IAzureBlobService
{
    private readonly IConfiguration _config;

    public AzureBlobService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<FileUploadResponse> UploadFileAsync(FileUploadRequest request)
    {
        var settings = _config.GetSection("AzureBlobStorage");
        var connectionString = settings["ConnectionString"];
        var containerName = settings["ContainerName"];
        var farmerDir = settings["FarmerDirectory"];
        var supplierDir = settings["SupplierDirectory"];
        var subDirs = settings.GetSection("SubDirectories");

        string baseDir = request.UserType.ToLower() == "farmer" ? farmerDir : supplierDir;
        string subDir = subDirs[request.FileCategory.First().ToString().ToUpper() + request.FileCategory.Substring(1).ToLower()];

        string blobName = $"{baseDir}/{subDir}/{request.UserId}_{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        using (var stream = request.File.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        return new FileUploadResponse
        {
            FileUrl = blobClient.Uri.ToString(),
            BlobName = blobName
        };
    }

    /// <summary>
    /// Delete a blob file from Azure Blob Storage by blob name.
    /// Safe operation - returns true even if blob doesn't exist.
    /// </summary>
    public async Task<bool> DeleteFileAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return false;

        try
        {
            var settings = _config.GetSection("AzureBlobStorage");
            var connectionString = settings["ConnectionString"];
            var containerName = settings["ContainerName"];

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
            return true;
        }
        catch
        {
            // Log the exception in production, but don't throw to prevent cascading failures
            return false;
        }
    }

    /// <summary>
    /// Delete multiple blob files from Azure Blob Storage.
    /// Safe operation - continues even if some blobs don't exist.
    /// Returns the count of successfully deleted blobs.
    /// </summary>
    public async Task<int> DeleteFilesAsync(params string[] blobNames)
    {
        if (blobNames == null || blobNames.Length == 0)
            return 0;

        int deletedCount = 0;
        var settings = _config.GetSection("AzureBlobStorage");
        var connectionString = settings["ConnectionString"];
        var containerName = settings["ContainerName"];

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        foreach (var blobName in blobNames)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                continue;

            try
            {
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
                deletedCount++;
            }
            catch
            {
                // Log the exception in production, but continue with other files
                continue;
            }
        }

        return deletedCount;
    }
}
