namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class LicenseUpdateViewModel
    {
        public string LicenseType { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public LicenseDocumentViewModel LicenseDocument { get; set; }
    }

    public class LicenseDocumentUpdateViewModel
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }

    public class AdditionalDocumentUpdateViewModel
    {
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }
}
