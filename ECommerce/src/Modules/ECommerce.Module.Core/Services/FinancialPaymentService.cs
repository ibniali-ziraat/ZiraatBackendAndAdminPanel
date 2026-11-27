using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class FinancialPaymentService : IFinancialPaymentService
    {
        private readonly IRepository<FinancialPayment> _paymentRepo;
        private readonly IRepository<BankDetails> _bankRepo;

        public FinancialPaymentService(
            IRepository<FinancialPayment> paymentRepo,
            IRepository<BankDetails> bankRepo)
        {
            _paymentRepo = paymentRepo;
            _bankRepo = bankRepo;
        }

        public async Task<IEnumerable<FinancialPayment>> GetAllAsync()
        {
            return await _paymentRepo.Query()
                .Include(x => x.BankDetails)
                .ToListAsync();
        }

        public async Task<FinancialPayment> GetByIdAsync(long id)
        {
            return await _paymentRepo.Query()
                .Include(x => x.BankDetails)
                .FirstOrDefaultAsync(x => x.SupplierProfileId == id);
        }

        public async Task AddAsync(FinancialPayment entity)
        {
            _paymentRepo.Add(entity);
            await _paymentRepo.SaveChangesAsync();
        }

        public async Task UpdateAsync(FinancialPayment entity)
        {
            await _paymentRepo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var existing = await GetByIdAsync(id);
            if (existing != null)
            {
                _paymentRepo.Remove(existing);
                await _paymentRepo.SaveChangesAsync();
            }
        }

        public async Task SaveWithBankDetailsAsync(FinancialPayment payment)
        {
            using var tx = _paymentRepo.BeginTransaction();
            try
            {
                if (payment.Id == 0)
                {
                    _paymentRepo.Add(payment);
                    await _paymentRepo.SaveChangesAsync();
                }
                else
                {
                    var existing = _paymentRepo.Query().FirstOrDefault(x => x.Id == payment.Id);
                    if (existing == null) throw new InvalidOperationException("FinancialPayment not found.");

                    existing.SupplierProfileId = payment.SupplierProfileId;
                    existing.UpiAvailable = payment.UpiAvailable;
                    existing.CreditFacility = payment.CreditFacility;
                    existing.SellingViaZiraat = payment.SellingViaZiraat;

                    await _paymentRepo.SaveChangesAsync();

                }



                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task AddBankDetailAsync(BankDetails bankDetail)
        {
            if (bankDetail == null) throw new ArgumentNullException(nameof(bankDetail));

            _bankRepo.Add(bankDetail);
            await _bankRepo.SaveChangesAsync();
        }

        public async Task UpdateBankDetailAsync(BankDetails bankDetail, long bankDetailId)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(bankDetail);

                var existing = _bankRepo.Query().FirstOrDefault(x => x.Id == bankDetailId) ?? throw new InvalidOperationException($"BankDetail with id {bankDetail.Id} not found.");
                existing.AccountHolderName = bankDetail.AccountHolderName;
                existing.AccountNumber = bankDetail.AccountNumber;
                existing.IFSCCode = bankDetail.IFSCCode;
                existing.BankName = bankDetail.BankName;
                existing.BranchName = bankDetail.BranchName;
                existing.IsActive = bankDetail.IsActive;

                await _bankRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating the bank detail.", ex);
            }
        }

        public async Task DeleteBankDetailAsync(long bankDetailId)
        {
            var existing = _bankRepo.Query().FirstOrDefault(x => x.Id == bankDetailId);
            if (existing == null) throw new InvalidOperationException($"BankDetail with id {bankDetailId} not found.");

            _bankRepo.Remove(existing);
            await _bankRepo.SaveChangesAsync();
        }

        public async Task ToggleBankDetailStatusAsync(long bankDetailId, bool isActive)
        {
            var bankDetail = _bankRepo.Query().FirstOrDefault(x => x.Id == bankDetailId);
            if (bankDetail == null) throw new InvalidOperationException($"BankDetail with id {bankDetailId} not found.");

            var allBankDetails = _bankRepo.Query().ToList();
            foreach (var bd in allBankDetails)
            {
                bd.IsActive = false;
            }

            bankDetail.IsActive = true;

            await _bankRepo.SaveChangesAsync();
        }
        public async Task<BankDetails> GetBankDetailsByIdAsync(long bankDetailId)
        {
            return await _bankRepo.Query().FirstOrDefaultAsync(x => x.Id == bankDetailId);
        }
    }
}
