namespace ECommerce.Module.Core.Models
{
    /// <summary>
    /// Lightweight product interface to support vendor product assignments
    /// Implemented by Catalog module without creating circular dependency
    /// </summary>
    public interface IProduct
    {
        long Id { get; }
        decimal Price { get; }
        string Name { get; }
        string Sku { get; }
        int StockQuantity { get; }
    }
}
