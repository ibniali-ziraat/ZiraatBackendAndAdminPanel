namespace ECommerce.Module.Search.Areas.Search.ViewModels;

public class VendorSearchResult : SearchResult
    {
        public long VendorId { get; set; }
        public string VendorName { get; set; }
        public string VendorSlug { get; set; }
    }

