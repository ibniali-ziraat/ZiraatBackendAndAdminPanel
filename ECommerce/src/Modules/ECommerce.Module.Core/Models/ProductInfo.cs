namespace ECommerce.Module.Core.Models
{
    /// <summary>
    /// Lightweight product information DTO to avoid circular reference with Catalog module
    /// </summary>
    public class ProductInfo
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public string Sku { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; }
    }
}
