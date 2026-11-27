using Microsoft.AspNetCore.Http;

namespace ECommerce.Module.Core.Areas.Core.ViewModels;

public class FileUploadRequest
{
    public IFormFile File { get; set; }

    /// <summary>
    /// User type. Possible values: "farmer", "supplier"
    /// </summary>
    public string UserType { get; set; } // "farmer" or "supplier"

    /// <summary>
    /// File category. Possible values: "documents", "images", "profile"
    /// </summary>
    public string FileCategory { get; set; } // "documents", "images", "profile"

    /// <summary>
    /// User ID for unique naming (optional)
    /// </summary>
    public string UserId { get; set; } // e.g. "12345"
}
