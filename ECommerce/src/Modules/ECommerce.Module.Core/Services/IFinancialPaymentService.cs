using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public interface IFinancialPaymentService
    {
        Task<IEnumerable<FinancialPayment>> GetAllAsync();
        Task<FinancialPayment> GetByIdAsync(long id);
        Task AddAsync(FinancialPayment entity);
        Task UpdateAsync(FinancialPayment entity);
        Task DeleteAsync(long id);

        // Save parent and its bank detail in one operation (create/update)
        Task SaveWithBankDetailsAsync(FinancialPayment payment);
        Task ToggleBankDetailStatusAsync(long bankDetailId, bool isActive);
        Task DeleteBankDetailAsync(long bankDetailId);
        Task AddBankDetailAsync(BankDetails bankDetail);
        Task UpdateBankDetailAsync(BankDetails bankDetail,long bankDetailId);
        Task<BankDetails> GetBankDetailsByIdAsync(long bankDetailId);


    }
}
