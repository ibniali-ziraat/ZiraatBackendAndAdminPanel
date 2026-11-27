using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;


namespace ECommerce.Module.Core.Services
{
    public interface IEducationService
    {
        Task<Education> GetByIdAsync(long id);
        Task<Education> GetByProfileIdAsync(long profileId);
        Task<IEnumerable<Education>> GetAllAsync();
        Task CreateAsync(EducationViewModel model);
        Task UpdateAsync(long id, EducationViewModel model);
        Task DeleteAsync(long id);
    }

    public class EducationService : IEducationService
    {
        private readonly IRepository<Education> _educationRepository;

        public EducationService(IRepository<Education> educationRepository)
        {
            _educationRepository = educationRepository;
        }

        public async Task<Education> GetByIdAsync(long id)
        {
            return await _educationRepository.Query().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Education> GetByProfileIdAsync(long profileId)
        {
            return await _educationRepository.Query().FirstOrDefaultAsync(x => x.ProfileId == profileId);
        }

        public async Task<IEnumerable<Education>> GetAllAsync()
        {
            return await _educationRepository.Query().ToListAsync();
        }

        public async Task CreateAsync(EducationViewModel model)
        {
            var entity = new Education
            {
                ProfileId = model.ProfileId,
                HighestQualification = model.HighestQualification,
                InstituteName = model.InstituteName,
                PassingYear = model.PassingYear,
                Specialization = model.Specialization
            };

            _educationRepository.Add(entity);
            await _educationRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(long id, EducationViewModel model)
        {
            var entity = await _educationRepository.Query().FirstOrDefaultAsync(x => x.ProfileId == id);
            if (entity == null) return;

            entity.ProfileId = model.ProfileId;
            entity.HighestQualification = model.HighestQualification;
            entity.InstituteName = model.InstituteName;
            entity.PassingYear = model.PassingYear;
            entity.Specialization = model.Specialization;

            await _educationRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _educationRepository.Query().FirstOrDefaultAsync(x => x.ProfileId == id);
            if (entity == null) return;

            _educationRepository.Remove(entity);
            await _educationRepository.SaveChangesAsync();
        }
    }
}
