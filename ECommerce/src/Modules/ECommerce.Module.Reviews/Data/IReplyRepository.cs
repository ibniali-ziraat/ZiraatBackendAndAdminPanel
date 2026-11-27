using ECommerce.Infrastructure.Data;
using ECommerce.Module.Reviews.Models;
using System.Linq;

namespace ECommerce.Module.Reviews.Data
{
    public interface IReplyRepository : IRepository<Reply>
    {
        IQueryable<ReplyListItemDto> List();
    }
}
