using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<FileUploadController> _logger;
    private readonly IMediaService _mediaService;
    public FileUploadController(IAzureBlobService blobService, ILogger<FileUploadController> logger, IMediaService mediaService)
    {
        _blobService = blobService;
        _logger = logger;
        _mediaService = mediaService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded.");

        try
        {
            //var result = await _blobService.UploadFileAsync(request);
            //return Ok(result);
            var originalFileName = ContentDispositionHeaderValue.Parse(request.File.ContentDisposition).FileName.ToString().Trim('"');
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            await _mediaService.SaveMediaAsync(request.File.OpenReadStream(), fileName, request.File.ContentType);

            return Ok(_mediaService.GetMediaUrl(fileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "Error uploading file. Please try again." });
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteFile([FromBody] DeleteBlobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.BlobName))
            return BadRequest(new { message = "BlobName is required." });

        try
        {
            var success = await _blobService.DeleteFileAsync(request.BlobName);
            return Ok(new { success, message = success ? "File deleted successfully." : "File not found or already deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {BlobName}", request.BlobName);
            return StatusCode(500, new { message = "Error deleting file." });
        }
    }

    [HttpPost("delete-multiple")]
    public async Task<IActionResult> DeleteFiles([FromBody] DeleteMultipleBlobsRequest request)
    {
        if (request?.BlobNames == null || request.BlobNames.Count == 0)
            return BadRequest(new { message = "At least one BlobName is required." });

        try
        {
            var deletedCount = await _blobService.DeleteFilesAsync(request.BlobNames.ToArray());
            return Ok(new
            {
                success = true,
                message = $"{deletedCount} file(s) deleted successfully.",
                deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple files");
            return StatusCode(500, new { message = "Error deleting files." });
        }
    }
}

public class DeleteBlobRequest
{
    public string BlobName { get; set; }
}

public class DeleteMultipleBlobsRequest
{
    public List<string> BlobNames { get; set; } = new List<string>();
}
