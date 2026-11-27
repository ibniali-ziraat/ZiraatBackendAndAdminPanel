using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;


namespace ECommerce.Module.Core.Services
{
    public interface ILandholdingService
    {
        Task<Landholding> GetByIdAsync(long id);
        Task<IEnumerable<Landholding>> GetAllAsync();
        Task CreateAsync(LandholdingViewModel model);
        Task UpdateAsync(long id, LandholdingViewModel model);
        Task DeleteAsync(long id);
        Task<Landholding> GetByProfileIdAsync(long profileId);
    }

    public class LandholdingService : ILandholdingService
    {
        private readonly IRepository<Landholding> _repo;

        public LandholdingService(IRepository<Landholding> repo)
        {
            _repo = repo;
        }

        public async Task<Landholding> GetByIdAsync(long id) => await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);

        public async Task<IEnumerable<Landholding>> GetAllAsync() => await _repo.Query().ToListAsync();

        public async Task CreateAsync(LandholdingViewModel model)
        {
            var entity = new Landholding
            {
                ProfileId = model.ProfileId,
                SoilType = model.SoilType,
                IrrigationMethod = model.IrrigationMethod,
                EnvironmentalConditions = model.EnvironmentalConditions,
                LandTopography = model.LandTopography
            };

            _repo.Add(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(long id, LandholdingViewModel model)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            entity.ProfileId = model.ProfileId;
            entity.SoilType = model.SoilType;
            entity.IrrigationMethod = model.IrrigationMethod;
            entity.EnvironmentalConditions = model.EnvironmentalConditions;
            entity.LandTopography = model.LandTopography;

            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            _repo.Remove(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task<Landholding> GetByProfileIdAsync(long profileId)
        {
            return await _repo.Query().FirstOrDefaultAsync(x => x.ProfileId == profileId);
        }
    }
}
