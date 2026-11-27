using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class OperationalDetailsService : IOperationalDetailsService
    {
        private readonly IRepository<OperationalDetails> _detailsRepo;
        private readonly IRepository<ServiceArea> _serviceAreaRepo;
        private readonly IRepository<CompanyTieup> _companyTieupRepo;

        public OperationalDetailsService(
            IRepository<OperationalDetails> detailsRepo,
            IRepository<ServiceArea> serviceAreaRepo,
            IRepository<CompanyTieup> companyTieupRepo)
        {
            _detailsRepo = detailsRepo;
            _serviceAreaRepo = serviceAreaRepo;
            _companyTieupRepo = companyTieupRepo;
        }

        public async Task<IEnumerable<OperationalDetails>> GetAllAsync()
        {
            return await _detailsRepo.Query()
                .Include(x => x.ServiceAreas)
                .Include(x => x.CompanyTieups)
                .ToListAsync();
        }

        public async Task<OperationalDetails> GetByIdAsync(long id)
        {
            return await _detailsRepo.Query()
                .Include(x => x.ServiceAreas)
                .Include(x => x.CompanyTieups)
                .FirstOrDefaultAsync(x => x.SupplierProfileId == id);
        }

        public async Task AddAsync(OperationalDetails entity)
        {
            _detailsRepo.Add(entity);
            await _detailsRepo.SaveChangesAsync();
        }

        public async Task UpdateAsync(OperationalDetails entity)
        {
            // tracked entity; just save
            await _detailsRepo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _detailsRepo.Remove(entity);
                await _detailsRepo.SaveChangesAsync();
            }
        }

        public async Task SaveWithDetailsAsync(OperationalDetails details, IEnumerable<ServiceArea> serviceAreas, IEnumerable<CompanyTieup> companyTieups)
        {
            using (var tx = _detailsRepo.BeginTransaction())
            {
                try
                {
                    if (details.Id == 0)
                    {
                        _detailsRepo.Add(details);
                        await _detailsRepo.SaveChangesAsync();
                    }
                    else
                    {
                        // Try to load existing by primary key (async). If not found, treat as create.
                        var existing = await _detailsRepo.Query().FirstOrDefaultAsync(p => p.Id == details.Id);
                        if (existing == null)
                        {
                            // fallback: create a new record when the requested Id does not exist
                            _detailsRepo.Add(details);
                            await _detailsRepo.SaveChangesAsync();
                        }
                        else
                        {
                            existing.YearsInBusiness = details.YearsInBusiness;
                            existing.DeliveryServiceAvailable = details.DeliveryServiceAvailable;
                            existing.AvgMonthlySalesVolume = details.AvgMonthlySalesVolume;
                            existing.SupplierProfileId = details.SupplierProfileId;
                            await _detailsRepo.SaveChangesAsync();

                            // remove existing child items by the FK property name used in models
                            var existingServiceAreas = _serviceAreaRepo.Query().Where(x => x.OperationalDetailsId == existing.Id).ToList();
                            foreach (var sa in existingServiceAreas) _serviceAreaRepo.Remove(sa);

                            var existingTieups = _companyTieupRepo.Query().Where(x => x.OperationalDetailsId == existing.Id).ToList();
                            foreach (var t in existingTieups) _companyTieupRepo.Remove(t);

                            await _serviceAreaRepo.SaveChangesAsync();
                            await _companyTieupRepo.SaveChangesAsync();
                        }
                    }

                    // At this point details.Id will be populated for newly created record
                    var parentId = details.Id;
                    // If we created a new details above, ensure parentId is set correctly (reload if necessary)
                    if (parentId == 0)
                    {
                        parentId = (await _detailsRepo.Query().OrderByDescending(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync());
                    }

                    // Add provided child items (set OperationalDetailsId)
                    foreach (var sa in serviceAreas ?? Enumerable.Empty<ServiceArea>())
                    {
                        sa.OperationalDetailsId = parentId;
                        _serviceAreaRepo.Add(sa);
                    }

                    foreach (var t in companyTieups ?? Enumerable.Empty<CompanyTieup>())
                    {
                        t.OperationalDetailsId = parentId;
                        _companyTieupRepo.Add(t);
                    }

                    // Save all
                    await _serviceAreaRepo.SaveChangesAsync();
                    await _companyTieupRepo.SaveChangesAsync();

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }
}
