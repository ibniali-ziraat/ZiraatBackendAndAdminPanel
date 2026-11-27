using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Data
{
    public class Repository<T> : RepositoryWithTypedId<T, long>, IRepository<T>
       where T : class, IEntityWithTypedId<long>
    {
        public Repository(SimplDbContext context) : base(context)
        {
        }
    }
}
