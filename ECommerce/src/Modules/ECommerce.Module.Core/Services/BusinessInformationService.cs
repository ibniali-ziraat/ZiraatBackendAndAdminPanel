using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class BusinessInformationService : IBusinessInformationService
    {
        private readonly IRepository<BusinessInformation> _repository;

        public BusinessInformationService(IRepository<BusinessInformation> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<BusinessInformation>> GetAllAsync()
        {
            return await _repository.Query().ToListAsync();
        }

        public async Task<BusinessInformation> GetByIdAsync(long id)
        {
            return await _repository.Query().FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task<BusinessInformation> GetBusinessInformationByUserId(long userId)
        {
            return await _repository.Query().FirstOrDefaultAsync(x => x.UserId == userId);
        }
        public async Task AddAsync(BusinessInformation entity)
        {
            _repository.Add(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task UpdateAsync(BusinessInformation entity)
        {
            // Entity is tracked by EF; Save changes to persist updates.
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _repository.Remove(entity);
                await _repository.SaveChangesAsync();
            }
        }
    }
}
