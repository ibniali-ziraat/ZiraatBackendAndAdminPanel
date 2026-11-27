using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;


namespace ECommerce.Module.Core.Services
{
    public interface IProfessionService
    {
        Task<Profession> GetByIdAsync(long id);
        Task<IEnumerable<Profession>> GetAllAsync();
        Task CreateAsync(ProfessionViewModel model);
        Task UpdateAsync(long id, ProfessionViewModel model);
        Task DeleteAsync(long id);
        Task<Profession> GetByProfileIdAsync(long profileId);

    }

    public class ProfessionService : IProfessionService
    {
        private readonly IRepository<Profession> _professionRepository;

        public ProfessionService(IRepository<Profession> professionRepository)
        {
            _professionRepository = professionRepository;
        }

        public async Task<Profession> GetByIdAsync(long id)
        {
            return await _professionRepository.Query()
                .FirstOrDefaultAsync(p => p.ProfileId == id);
        }

        public async Task<IEnumerable<Profession>> GetAllAsync()
        {
            return await _professionRepository.Query().ToListAsync();
        }

        public async Task CreateAsync(ProfessionViewModel model)
        {
            if (model == null)
                return;

            var entity = new Profession
            {
                ProfileId = model.ProfileId,
                ProfessionType = model.ProfessionType,
                OrganizationName = model.OrganizationName,
                Designation = model.Designation,
                ExperienceYears = model.ExperienceYears,
                Skills = model.Skills
            };

            _professionRepository.Add(entity);
            await _professionRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(long id, ProfessionViewModel model)
        {
            var entity = await _professionRepository.Query().FirstOrDefaultAsync(p => p.ProfileId  == id);
            if (entity == null)
                return;

            entity.ProfessionType = model.ProfessionType;
            entity.OrganizationName = model.OrganizationName;
            entity.Designation = model.Designation;
            entity.ExperienceYears = model.ExperienceYears;
            entity.Skills = model.Skills;

            await _professionRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _professionRepository.Query().FirstOrDefaultAsync(p => p.ProfileId == id);
            if (entity == null)
                return;

            _professionRepository.Remove(entity);
            await _professionRepository.SaveChangesAsync();
        }

        public async Task<Profession> GetByProfileIdAsync(long profileId)
        {
            return await _professionRepository.Query()
                  .FirstOrDefaultAsync(p => p.ProfileId == profileId);
        }
    }
}
