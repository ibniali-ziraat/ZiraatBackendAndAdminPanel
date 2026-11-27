using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class ProductPortfolioService : IProductPortfolioService
    {
        private readonly IRepository<ProductPortfolio> _portfolioRepo;
        private readonly IRepository<ProductInputType> _inputTypeRepo;
        private readonly IRepository<ProductMajorBrand> _brandRepo;
        private readonly IRepository<ProductCustomInput> _customRepo;

        public ProductPortfolioService(
            IRepository<ProductPortfolio> portfolioRepo,
            IRepository<ProductInputType> inputTypeRepo,
            IRepository<ProductMajorBrand> brandRepo,
            IRepository<ProductCustomInput> customRepo)
        {
            _portfolioRepo = portfolioRepo;
            _inputTypeRepo = inputTypeRepo;
            _brandRepo = brandRepo;
            _customRepo = customRepo;
        }

        public async Task<IEnumerable<ProductPortfolio>> GetAllAsync() =>
            await _portfolioRepo.Query()
                .Include(p => p.InputTypes)
                .Include(p => p.MajorBrands)
                .Include(p => p.CustomInputs)
                .ToListAsync();

        public async Task<ProductPortfolio> GetByIdAsync(long id) =>
            await _portfolioRepo.Query()
                .Include(p => p.InputTypes)
                .Include(p => p.MajorBrands)
                .Include(p => p.CustomInputs)
                .FirstOrDefaultAsync(x => x.SupplierProfileId == id);

        public async Task<ProductPortfolio> GetBySupplierProfileIdAsync(long supplierProfileId) =>
            await _portfolioRepo.Query()
                .Include(p => p.InputTypes)
                .Include(p => p.MajorBrands)
                .Include(p => p.CustomInputs)
                .FirstOrDefaultAsync(x => x.SupplierProfileId == supplierProfileId);

        public async Task AddAsync(ProductPortfolio entity)
        {
            _portfolioRepo.Add(entity);
            await _portfolioRepo.SaveChangesAsync();
        }

        public async Task UpdateAsync(ProductPortfolio entity)
        {
            // tracked entity update, commit
            await _portfolioRepo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _portfolioRepo.Remove(entity);
                await _portfolioRepo.SaveChangesAsync();
            }
        }

        public async Task SaveWithDetailsAsync(ProductPortfolio portfolio, IEnumerable<ProductInputType> inputTypes, IEnumerable<ProductMajorBrand> majorBrands, IEnumerable<ProductCustomInput> customInputs)
        {
            using (var tx = _portfolioRepo.BeginTransaction())
            {
                try
                {
                    if (portfolio.Id == 0)
                    {
                        _portfolioRepo.Add(portfolio);
                        await _portfolioRepo.SaveChangesAsync();
                    }
                    else
                    {
                        // ensure portfolio is attached/tracked
                        var existing = _portfolioRepo.Query().FirstOrDefault(p => p.Id == portfolio.Id);
                        if (existing == null) throw new InvalidOperationException("Portfolio not found.");
                        existing.StorageCapacityValue = portfolio.StorageCapacityValue;
                        existing.StorageCapacityUnit = portfolio.StorageCapacityUnit;
                        existing.SupplierProfileId = portfolio.SupplierProfileId;
                        await _portfolioRepo.SaveChangesAsync();

                        // remove existing child items
                        var existingInputs = _inputTypeRepo.Query().Where(x => x.ProductPortfolioId == portfolio.Id).ToList();
                        foreach (var it in existingInputs) _inputTypeRepo.Remove(it);

                        var existingBrands = _brandRepo.Query().Where(x => x.ProductPortfolioId == portfolio.Id).ToList();
                        foreach (var b in existingBrands) _brandRepo.Remove(b);

                        var existingCustoms = _customRepo.Query().Where(x => x.ProductPortfolioId == portfolio.Id).ToList();
                        foreach (var c in existingCustoms) _customRepo.Remove(c);

                        await _inputTypeRepo.SaveChangesAsync();
                        await _brandRepo.SaveChangesAsync();
                        await _customRepo.SaveChangesAsync();
                    }

                    // Add provided child items (set ProductPortfolioId)
                    foreach (var it in inputTypes ?? Enumerable.Empty<ProductInputType>())
                    {
                        it.ProductPortfolioId = portfolio.Id;
                        _inputTypeRepo.Add(it);
                    }

                    foreach (var b in majorBrands ?? Enumerable.Empty<ProductMajorBrand>())
                    {
                        b.ProductPortfolioId = portfolio.Id;
                        _brandRepo.Add(b);
                    }

                    foreach (var c in customInputs ?? Enumerable.Empty<ProductCustomInput>())
                    {
                        c.ProductPortfolioId = portfolio.Id;
                        _customRepo.Add(c);
                    }

                    // Save all
                    await _inputTypeRepo.SaveChangesAsync();
                    await _brandRepo.SaveChangesAsync();
                    await _customRepo.SaveChangesAsync();

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
