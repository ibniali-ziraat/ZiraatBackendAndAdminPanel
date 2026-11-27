using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;


namespace ECommerce.Module.Core.Services
{
    public interface IDemographicsService
    {
        Task<Demographics> GetByIdAsync(long id);
        Task<IEnumerable<Demographics>> GetAllAsync();
        Task CreateAsync(DemographicsViewModel model);
        Task UpdateAsync(long id, DemographicsViewModel model);
        Task DeleteAsync(long id);
        Task<Demographics> GetByProfileIdAsync(long profileId);
    }

    public class DemographicsService : IDemographicsService
    {
        private readonly IRepository<Demographics> _repo;

        public DemographicsService(IRepository<Demographics> repo)
        {
            _repo = repo;
        }

        public async Task<Demographics> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IEnumerable<Demographics>> GetAllAsync() => await _repo.Query().ToListAsync();

        public async Task CreateAsync(DemographicsViewModel model)
        {
            var entity = new Demographics
            {
                ProfileId = model.ProfileId,
                FarmName = model.FarmName,
                FarmSize = model.FarmSize,
                MainCrops = model.MainCrops,
                OwnershipType = model.OwnershipType
            };

            _repo.Add(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(long id, DemographicsViewModel model)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            entity.ProfileId = model.ProfileId;
            entity.FarmName = model.FarmName;
            entity.FarmSize = model.FarmSize;
            entity.MainCrops = model.MainCrops;
            entity.OwnershipType = model.OwnershipType;

            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            _repo.Remove(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task<Demographics> GetByProfileIdAsync(long profileId)
        {
            return await _repo.Query().FirstOrDefaultAsync(x => x.ProfileId == profileId);
        }
    }
}
