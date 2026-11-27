using System.Linq;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Comments.Models;

namespace ECommerce.Module.Comments.Data
{
    public interface ICommentRepository : IRepository<Comment>
    {
        IQueryable<CommentListItemDto> List();
    }
}