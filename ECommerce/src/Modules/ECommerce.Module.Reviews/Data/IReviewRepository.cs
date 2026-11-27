using ECommerce.Infrastructure.Data;
using ECommerce.Module.Reviews.Models;
using System.Linq;

namespace ECommerce.Module.Reviews.Data
{
    public interface IReviewRepository : IRepository<Review>
    {
        IQueryable<ReviewListItemDto> List();
    }
}