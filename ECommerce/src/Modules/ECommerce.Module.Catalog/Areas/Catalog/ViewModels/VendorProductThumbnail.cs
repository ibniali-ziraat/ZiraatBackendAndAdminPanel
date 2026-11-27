namespace ECommerce.Module.Catalog.Areas.Catalog.ViewModels;

public class VendorProductThumbnail
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public decimal? AssignedPrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string ThumbnailUrl { get; set; }
    public string VendorName { get; set; }
    public string VendorSlug { get; set; }
}
