using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public interface IVendorCustomerAssociationService
    {
        /// <summary>
        /// Searches for a customer by phone number
        /// </summary>
        Task<User> SearchCustomerByPhoneNumber(string phoneNumber);

        /// <summary>
        /// Adds a new customer association for a vendor
        /// </summary>
        Task AddCustomer(long vendorId, AddCustomerVm model, long createdById);

        /// <summary>
        /// Update customer association for a vendor
        /// </summary>
        Task UpdateCustomer(long vendorId, UpdateCustomerVm model, long createdById);

        /// <summary>
        /// Update customer balance for a vendor
        /// </summary>
        Task UpdateCustomerBalance(long vendorId, UpdateCustomerBalance model);


        /// <summary>
        /// Deletes a customer association
        /// </summary>
        Task DeleteCustomer(long vendorId, long customerId);

        /// <summary>
        /// Gets all associated customers for a vendor
        /// </summary>
        Task<List<GetAllCustomersVm>> GetAllCustomers(long vendorId);

        /// <summary>
        /// Links or unlinks a customer to/from a vendor
        /// </summary>
        Task LinkUnlinkCustomer(long vendorId, long customerId, bool linkUnlink, long updatedById);

        /// <summary>
        /// Gets all orders for a specific customer from a vendor
        /// </summary>
        Task<GetCustomerOrdersVm> GetCustomerOrders(long vendorId, long customerId);

        /// <summary>
        /// Gets comprehensive BuyOnCredit orders with calculated totals and settlements
        /// </summary>
        Task<GetCustomerOrdersVm> GetCustomerBuyOnCreditOrders(long vendorId, long customerId);

        /// <summary>
        /// Adds a settlement record for a customer
        /// </summary>
        Task AddSettlement(long vendorId, long customerId, AddSettlementVm model, long createdById);

        /// <summary>
        /// Gets settlement summary and history for a customer
        /// </summary>
        Task<GetSettlementSummaryVm> GetSettlementSummary(long vendorId, long customerId);

        /// <summary>
        /// Calculates total balance from orders bought on credit
        /// </summary>
        Task<decimal> CalculateCustomerBalance(long vendorId, long customerId);
    }
}
