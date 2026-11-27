using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Models;
using ECommerce.Module.Core.Areas.Core.ViewModels;

namespace ECommerce.Module.Core.Services
{
    public class VendorCustomerAssociationService : IVendorCustomerAssociationService
    {
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<CustomerSettlement> _customerSettlementRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Profile> _profileRepository;
        private readonly IOrderService _orderService;


        public VendorCustomerAssociationService(
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<CustomerSettlement> customerSettlementRepository,
            IRepository<User> userRepository,
            IRepository<Profile> profileRepository,
            IOrderService orderService)
        {
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _customerSettlementRepository = customerSettlementRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _orderService = orderService;
        }

        public async Task<User> SearchCustomerByPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));
            }

            var customer = await _userRepository.Query()
                .Where(u => u.PhoneNumber == phoneNumber && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (customer != null)
            {
                return customer;
            }

            var profile = await _profileRepository.Query()
                .Where(p => p.Phone == phoneNumber)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                return null;
            }

            customer = await _userRepository.Query()
                .Where(u => u.Id == profile.CoreUserId && !u.IsDeleted)
                .FirstOrDefaultAsync();

            if (customer != null && string.IsNullOrWhiteSpace(customer.PhoneNumber))
            {
                customer.PhoneNumber = profile.Phone;
            }

            return customer;
        }

        public async Task AddCustomer(long vendorId, AddCustomerVm model, long createdById)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Mobile))
            {
                throw new ArgumentException("Name, Email, and Mobile are required");
            }

            // Check if customer already exists with this email/phone
            var existingCustomer = await _userRepository.Query()
                .FirstOrDefaultAsync(u => (u.Id == model.CustomerId || u.PhoneNumber == model.Mobile) && !u.IsDeleted);

            if (existingCustomer == null)
            {
                throw new InvalidOperationException("Customer not found");
            }

            // Check if association already exists
            var existingAssociation = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == model.CustomerId && !v.IsDeleted);

            if (existingAssociation != null)
            {
                throw new InvalidOperationException("This customer is already associated with this vendor");
            }

            var association = new VendorCustomerAssociation
            {
                VendorId = vendorId,
                CustomerId = model.CustomerId,
                CustomerName = model.Name,
                CustomerEmail = model.Email,
                CustomerPhoneNumber = model.Mobile,
                AllowCredit = model.AllowCredit,
                MaxCreditAmount = model.MaxCreditAmount,
                CurrentBalance = 0,
                StartDate = model.StartDate,
                IsActive = true,
                CreatedOn = DateTimeOffset.Now,
                CreatedById = createdById,
                LatestUpdatedOn = DateTimeOffset.Now,
                LatestUpdatedById = createdById,
                IsDeleted = false
            };
            _vendorCustomerAssociationRepository.Add(association);
            await _vendorCustomerAssociationRepository.SaveChangesAsync();
        }
        public async Task UpdateCustomer(long vendorId, UpdateCustomerVm model, long createdById)
        {
            

            // Check if association already exists
            var existingAssociation = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == model.CustomerId && !v.IsDeleted);

            if (existingAssociation == null)
            {
                throw new InvalidOperationException("No customer association found");
            }


            existingAssociation.AllowCredit = model.AllowCredit;
            existingAssociation.MaxCreditAmount = model.MaxCreditAmount;
                
            await _vendorCustomerAssociationRepository.SaveChangesAsync();
        }
        public async Task UpdateCustomerBalance(long vendorId, UpdateCustomerBalance model)
        {


            // Check if association already exists
            var existingAssociation = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == model.CustomerId && !v.IsDeleted);

            if (existingAssociation == null)
            {
                throw new InvalidOperationException("No customer association found");
            }


            existingAssociation.CurrentBalance = model.CurrentBalance;

            await _vendorCustomerAssociationRepository.SaveChangesAsync();
        }
        public async Task DeleteCustomer(long vendorId, long customerId)
        {
            var association = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == customerId && !v.IsDeleted);

            if (association == null)
            {
                throw new InvalidOperationException("Customer association not found");
            }

            var hasSettlement = await _customerSettlementRepository.Query()
                .AnyAsync(s => s.VendorCustomerAssociationId == association.Id);

            if (hasSettlement)
            {
                throw new ArgumentException("Customer cannot be deleted because there are records associated with this customer.");
            }

            association.IsDeleted = true;
            association.LatestUpdatedOn = DateTimeOffset.Now;
            _vendorCustomerAssociationRepository.Remove(association);
            await _vendorCustomerAssociationRepository.SaveChangesAsync();
        }

        public async Task<List<GetAllCustomersVm>> GetAllCustomers(long vendorId)
        {
            var customers = await _vendorCustomerAssociationRepository.Query()
                .Where(v => v.VendorId == vendorId && !v.IsDeleted)
                .Select(v => new GetAllCustomersVm
                {
                    UserId= v.Customer.Id,   
                    CustomerId = v.CustomerId,
                    CustomerName = v.CustomerName,
                    CustomerPhoneNumber = v.CustomerPhoneNumber,
                    CustomerEmail = v.CustomerEmail,
                    Credit = v.MaxCreditAmount,
                    Balance = v.CurrentBalance,
                    StartDate = v.StartDate,
                    ActiveInactive = v.IsActive
                })
                .ToListAsync();

            if (_orderService != null)
            {
                foreach (var customer in customers)
                {
                    var customerOrders = await _orderService.GetCustomerOrders(vendorId, customer.CustomerId);
                    customer.TotalOrderAmount = SumDistinctBuyOnCreditOrderAmounts(customerOrders);
                }
            }

            return customers;
        }

        public async Task LinkUnlinkCustomer(long vendorId, long customerId, bool linkUnlink, long updatedById)
        {
            var association = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == customerId && !v.IsDeleted);

            if (association == null)
            {
                throw new InvalidOperationException("Customer association not found");
            }

            association.IsActive = linkUnlink;
            association.LatestUpdatedOn = DateTimeOffset.Now;
            association.LatestUpdatedById = updatedById;
            await _vendorCustomerAssociationRepository.SaveChangesAsync();
        }

        public async Task<GetCustomerOrdersVm> GetCustomerOrders(long vendorId, long customerId)
        {
            var orders = new List<CustomerOrderVm>();

            // Use order service if available. This endpoint returns all orders regardless of payment method.
            if (_orderService != null)
            {
                var orderDtos = await _orderService.GetCustomerOrders(vendorId, customerId);

                orders = orderDtos
                    .Select(o => new CustomerOrderVm
                    {
                        OrderId = o.OrderId,
                        ItemName = o.ItemName,
                        ItemQuantity = o.ItemQuantity,
                        OrderedDate = o.OrderedDate,
                        OrderAmount = o.OrderAmount,
                        OrderStatus = o.OrderStatus,
                        PaymentMethod = o.PaymentMethod
                    }).ToList();
            }

            return new GetCustomerOrdersVm
            {
                VendorId = vendorId,
                CustomerId = customerId,
                Orders = orders
            };
        }

        /// <summary>
        /// Get BuyOnCredit specific customer orders with calculated totals and settlements
        /// </summary>
        public async Task<GetCustomerOrdersVm> GetCustomerBuyOnCreditOrders(long vendorId, long customerId)
        {
            var orders = new List<CustomerOrderVm>();
            decimal totalCreditOrders = 0;
            int totalCreditOrderCount = 0;
            decimal totalSettledAmount = 0;

            // Get all customer orders filtered by BuyOnCredit payment method
            if (_orderService != null)
            {
                var orderDtos = await _orderService.GetCustomerOrders(vendorId, customerId);

                // Filter only BuyOnCredit orders
                var buyOnCreditOrders = orderDtos
                    .Where(o => o.PaymentMethod == "BuyOnCredit")
                    .ToList();

                orders = buyOnCreditOrders.Select(o => new CustomerOrderVm
                {
                    OrderId = o.OrderId,
                    ItemName = o.ItemName,
                    ItemQuantity = o.ItemQuantity,
                    OrderedDate = o.OrderedDate,
                    OrderAmount = o.OrderAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentMethod = o.PaymentMethod
                }).ToList();

                // Calculate totals from BuyOnCredit orders
                totalCreditOrders = SumDistinctOrderAmounts(buyOnCreditOrders);
                totalCreditOrderCount = buyOnCreditOrders.Select(o => o.OrderId).Distinct().Count();
            }

            // Get settlements only for BuyOnCredit orders
            var settlements = await _customerSettlementRepository.Query()
                .Where(s => s.VendorId == vendorId && s.CustomerId == customerId && !s.IsDeleted)
                .OrderByDescending(s => s.SettlementDate)
                .ToListAsync();

            // Calculate total settled amount
            totalSettledAmount = settlements.Sum(s => s.AmountPaid);

            var orderVm = new GetCustomerOrdersVm
            {
                VendorId = vendorId,
                CustomerId = customerId,
                Orders = orders,
                TotalCreditOrders = totalCreditOrderCount,
                TotalCreditOrderAmount = totalCreditOrders,
                TotalSettledAmount = totalSettledAmount,
                RemainingBalance = totalCreditOrders - totalSettledAmount
            };

            return orderVm;
        }

        public async Task AddSettlement(long vendorId, long customerId, AddSettlementVm model, long createdById)
        {
            var association = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == customerId && !v.IsDeleted);

            if (association == null)
            {
                throw new InvalidOperationException("Customer association not found");
            }

            // Determine payment method from the list
            var paymentMethod = DeterminePaymentMethod(model.PaymentMethod);
            var transactionId = GetTransactionId(model.PaymentMethod);

            // Update customer balance
            var newBalance = association.CurrentBalance - model.AmountPaid;
            if (newBalance < 0)
            {
                newBalance = 0;
            }

            association.CurrentBalance = newBalance;
            association.LatestUpdatedOn = DateTimeOffset.Now;
            association.LatestUpdatedById = createdById;
            await _vendorCustomerAssociationRepository.SaveChangesAsync();

            var settlement = new CustomerSettlement
            {
                VendorId = vendorId,
                VendorCustomerAssociationId = association.Id,
                CustomerId = customerId,
                AmountPaid = model.AmountPaid,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId,
                BalanceAfter = newBalance,
                SettlementDate = model.Date,
                SettlementReminderDate = model.SettlementReminderDate,
                Note = model.Note,
                CreatedOn = DateTimeOffset.Now,
                CreatedById = createdById,
                LatestUpdatedOn = DateTimeOffset.Now,
                LatestUpdatedById = createdById,
                IsDeleted = false
            };
            _customerSettlementRepository.Add(settlement);
            await _customerSettlementRepository.SaveChangesAsync();
        }

        public async Task<GetSettlementSummaryVm> GetSettlementSummary(long vendorId, long customerId)
        {
            var association = await _vendorCustomerAssociationRepository.Query()
                .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == customerId && !v.IsDeleted);

            if (association == null)
            {
                throw new InvalidOperationException("Customer association not found");
            }

            var settlements = await _customerSettlementRepository.Query()
                .Where(s => s.VendorId == vendorId && s.CustomerId == customerId && !s.IsDeleted)
                .OrderByDescending(s => s.SettlementDate)
                .ToListAsync();

            decimal totalAmount = 0;
            decimal totalPaid = 0;
            var totalBuyOnCreditOrders = 0;

            if (_orderService != null)
            {
                var orderDtos = await _orderService.GetCustomerOrders(vendorId, customerId);
                var buyOnCreditOrders = orderDtos
                    .Where(o => string.Equals(o.PaymentMethod, "BuyOnCredit", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                totalBuyOnCreditOrders = buyOnCreditOrders.Select(o => o.OrderId).Distinct().Count();
                totalAmount = SumDistinctOrderAmounts(buyOnCreditOrders);
                totalPaid = settlements.Sum(s => s.AmountPaid);
            }
            else
            {
                // Fall back to using association's max credit amount
                totalAmount = association.MaxCreditAmount;
                totalPaid = settlements.Sum(s => s.AmountPaid);
            }

            var remainingAmount = totalAmount - totalPaid;

            var settlementHistory = settlements.Select(s => new SettlementHistoryItemVm
            {
                SettlementId = s.Id,
                OrderId = 0,
                OrderedDate = s.CreatedOn.DateTime,
                AmountPaid = s.AmountPaid,
                PaymentMethod = s.PaymentMethod,
                BalanceAfter = s.BalanceAfter,
                TransactionId = s.TransactionId,
                Note = s.Note
            }).ToList();

            return new GetSettlementSummaryVm
            {
                TotalOrders = totalBuyOnCreditOrders,
                TotalAmount = totalAmount,
                RemainingAmount = remainingAmount > 0 ? remainingAmount : 0,
                SettlementHistory = settlementHistory
            };
        }

        public async Task<decimal> CalculateCustomerBalance(long vendorId, long customerId)
        {
            if (_orderService == null)
            {
                // Fall back to association's current balance
                var association = await _vendorCustomerAssociationRepository.Query()
                    .FirstOrDefaultAsync(v => v.VendorId == vendorId && v.CustomerId == customerId && !v.IsDeleted);

                return association?.CurrentBalance ?? 0;
            }

            var orderAmount = await _orderService.GetCustomerTotalOrderAmount(vendorId, customerId);
            var settledAmount = await _orderService.GetCustomerTotalSettledAmount(vendorId, customerId);

            return orderAmount - settledAmount;
        }

        private string DeterminePaymentMethod(List<SettlementPaymentMethodVm> paymentMethods)
        {
            if (paymentMethods == null || paymentMethods.Count == 0)
            {
                return "cash";
            }

            var method = paymentMethods.FirstOrDefault();
            if (method.Cash.HasValue && method.Cash.Value > 0)
            {
                return "cash";
            }
            else if (method.Online.HasValue && method.Online.Value > 0)
            {
                return "online";
            }
            else if (method.Cheque.HasValue && method.Cheque.Value > 0)
            {
                return "cheque";
            }

            return "cash";
        }

        private string GetTransactionId(List<SettlementPaymentMethodVm> paymentMethods)
        {
            if (paymentMethods == null || paymentMethods.Count == 0)
            {
                return null;
            }

            return paymentMethods.FirstOrDefault()?.TransactionId;
        }

        private static decimal SumDistinctBuyOnCreditOrderAmounts(IEnumerable<OrderDto> orders)
        {
            return SumDistinctOrderAmounts(orders
                .Where(o => string.Equals(o.PaymentMethod, "BuyOnCredit", StringComparison.OrdinalIgnoreCase)));
        }

        private static decimal SumDistinctOrderAmounts(IEnumerable<OrderDto> orders)
        {
            return orders
                .GroupBy(o => o.OrderId)
                .Sum(g => g.First().OrderAmount);
        }
    }
}
