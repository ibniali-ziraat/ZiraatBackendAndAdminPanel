using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data
{
    public interface ICustomModelBuilder
    {
        void Build(ModelBuilder modelBuilder);
    }
}
