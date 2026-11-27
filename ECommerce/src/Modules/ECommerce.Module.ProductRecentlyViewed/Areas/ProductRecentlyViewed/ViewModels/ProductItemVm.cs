namespace ECommerce.Module.ProductRecentlyViewed.Areas.ProductRecentlyViewed.ViewModels
{
    public class ProductItemVm
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        public decimal? SpecialPrice { get; set; }

        public string ShortDescription { get; set; }

        public int ReviewsCount { get; set; }

        public double? RatingAverage { get; set; }

        public string ThumbnailImageUrl { get; set; }
    }
}
