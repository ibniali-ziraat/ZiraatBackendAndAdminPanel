using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;


namespace ECommerce.Module.Core.Services
{
    public interface IAddressService
    {
        Task<UserProfileAddress> GetByIdAsync(long id);
        Task<UserProfileAddress> GetByProfileIdAsync(long profileId);
        Task<IEnumerable<UserProfileAddress>> GetAllAsync();
        Task CreateAsync(UserProfileAddressViewModel model);
        Task UpdateAsync(long id, UserProfileAddressViewModel model);
        Task DeleteAsync(long id);
    }

    public class AddressService : IAddressService
    {
        private readonly IRepository<UserProfileAddress> _repo;

        public AddressService(IRepository<UserProfileAddress> repo)
        {
            _repo = repo;
        }

        public async Task<UserProfileAddress> GetByIdAsync(long id)
        {
            return await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<UserProfileAddress> GetByProfileIdAsync(long profileId)
        {
            return await _repo.Query().FirstOrDefaultAsync(x => x.ProfileId == profileId);
        }

        public async Task<IEnumerable<UserProfileAddress>> GetAllAsync()
        {
            return await _repo.Query().ToListAsync();
        }

        public async Task CreateAsync(UserProfileAddressViewModel model)
        {
            var entity = new UserProfileAddress
            {
                ProfileId = model.ProfileId,
                Village = model.Village,
                Tehsil = model.Tehsil,
                District = model.District,
                State = model.State,
                Pincode = model.Pincode,
                Gps = model.Gps
            };

            _repo.Add(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(long id, UserProfileAddressViewModel model)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.ProfileId == id);
            if (entity == null) return;

            entity.ProfileId = model.ProfileId;
            entity.Village = model.Village;
            entity.Tehsil = model.Tehsil;
            entity.District = model.District;
            entity.State = model.State;
            entity.Pincode = model.Pincode;
            entity.Gps = model.Gps;

            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.ProfileId == id);
            if (entity == null) return;

            _repo.Remove(entity);
            await _repo.SaveChangesAsync();
        }
    }
}
