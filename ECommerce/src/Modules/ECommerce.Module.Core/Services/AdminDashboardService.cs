using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Data;
using ECommerce.Module.Core.Extensions;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.Core.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private static readonly string[] RegistrationMonths = { "Jan", "Feb", "Mar", "Apr" };
        private static readonly string[] DefaultOrderRegions = { "North", "South", "East", "West" };
        private static readonly string[] DefaultCustomerRegions = { "Srinagar", "Anantnag", "Baramulla", "Pulwama" };

        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<CustomerSettlement> _customerSettlementRepository;
        private readonly SimplDbContext _dbContext;
        private readonly IWorkContext _workContext;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(
            IRepository<User> userRepository,
            IRepository<UserAddress> userAddressRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<CustomerSettlement> customerSettlementRepository,
            SimplDbContext dbContext,
            IWorkContext workContext,
            ILogger<AdminDashboardService> logger)
        {
            _userRepository = userRepository;
            _userAddressRepository = userAddressRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _customerSettlementRepository = customerSettlementRepository;
            _dbContext = dbContext;
            _workContext = workContext;
            _logger = logger;
        }

        public async Task<AdminDashboardVm> GetDashboard()
        {
            var users = await _userRepository.Query()
                .Include(x => x.Roles)
                    .ThenInclude(x => x.Role)
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var customerUsers = users.Where(IsCustomer).ToList();
            if (customerUsers.Count == 0)
            {
                customerUsers = users.Where(x => !HasRole(x, "admin") && !HasRole(x, "vendor")).ToList();
            }

            var currentUser = await _workContext.GetCurrentUser();
            var customerIds = customerUsers.Select(x => x.Id).ToList();
            var customerRegions = await GetCustomerRegions(customerIds);
            var orderCounts = await GetOrderCountsByCustomer(customerIds);
            var associationCounts = await GetAssociationCountsByCustomer(customerIds);
            var transactionCounts = await GetTransactionCountsByCustomer(customerIds);
            var associations = await GetAssociations(customerIds);

            var verifiedCustomerCount = customerUsers.Count(IsVerified);
            var model = new AdminDashboardVm
            {
                CurrentUserName = GetDisplayName(currentUser),
                CurrentUserInitials = GetInitials(currentUser)
            };

            model.Metrics = new List<AdminDashboardMetricVm>
            {
                new AdminDashboardMetricVm { Label = "Active Customers", Value = customerUsers.Count.ToString(), IconCssClass = "fa fa-building-o" },
                new AdminDashboardMetricVm { Label = "Active Regions", Value = customerRegions.Values.Where(x => x != "N/A").Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(), IconCssClass = "fa fa-map-marker" },
                new AdminDashboardMetricVm { Label = "Verified Customers", Value = verifiedCustomerCount.ToString(), IconCssClass = "fa fa-truck" },
                new AdminDashboardMetricVm { Label = "Support Staff", Value = users.Count(x => HasRole(x, "admin") || HasRole(x, "vendor")).ToString(), IconCssClass = "fa fa-users" }
            };

            model.CustomerRegistrations = BuildMonthlyRegistrations(customerUsers);
            model.CustomerVerificationRates = BuildMonthlyVerificationRates(customerUsers);
            model.OrdersByRegion = await GetOrdersByRegion();
            model.CustomersByRegion = BuildCustomersByRegion(customerRegions);
            model.CustomerActivity = BuildCustomerActivity(customerUsers, associations, customerRegions, orderCounts, associationCounts, transactionCounts);

            return model;
        }

        private static bool HasRole(User user, string role)
        {
            return user.Roles.Any(x => string.Equals(x.Role?.Name, role, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCustomer(User user)
        {
            return HasRole(user, "customer");
        }

        private static bool IsVerified(User user)
        {
            return user.EmailConfirmed || user.PhoneNumberConfirmed;
        }

        private static string GetDisplayName(User user)
        {
            if (!string.IsNullOrWhiteSpace(user?.FullName))
            {
                return user.FullName;
            }

            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                return user.Email;
            }

            return "Admin User";
        }

        private static string GetInitials(User user)
        {
            var name = GetDisplayName(user);
            var initials = string.Concat(name
                .Split(new[] { ' ', '.', '@', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(x => char.ToUpperInvariant(x[0])));

            return string.IsNullOrWhiteSpace(initials) ? "A" : initials;
        }

        private static IList<AdminDashboardListItemVm> BuildMonthlyRegistrations(IList<User> customerUsers)
        {
            var year = DateTimeOffset.Now.Year;
            return RegistrationMonths.Select((month, index) =>
            {
                var monthNumber = index + 1;
                var count = customerUsers.Count(x => x.CreatedOn.Year == year && x.CreatedOn.Month == monthNumber);
                return new AdminDashboardListItemVm { Label = month, Value = count.ToString() };
            }).ToList();
        }

        private static IList<AdminDashboardListItemVm> BuildMonthlyVerificationRates(IList<User> customerUsers)
        {
            var year = DateTimeOffset.Now.Year;
            return RegistrationMonths.Select((month, index) =>
            {
                var monthNumber = index + 1;
                var customersInMonth = customerUsers.Where(x => x.CreatedOn.Year == year && x.CreatedOn.Month == monthNumber).ToList();
                var rate = customersInMonth.Count == 0 ? 0 : (int)Math.Round(customersInMonth.Count(IsVerified) * 100m / customersInMonth.Count);
                return new AdminDashboardListItemVm { Label = month, Value = $"{rate}%" };
            }).ToList();
        }

        private IList<AdminDashboardListItemVm> BuildCustomersByRegion(IDictionary<long, string> customerRegions)
        {
            var rows = customerRegions.Values
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != "N/A")
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .Take(4)
                .Select(x => new AdminDashboardListItemVm { Label = x.Key, Value = x.Count().ToString() })
                .ToList();

            return EnsureRows(rows, DefaultCustomerRegions, "0");
        }

        private IList<AdminDashboardCustomerActivityVm> BuildCustomerActivity(
            IList<User> customerUsers,
            IList<VendorCustomerAssociation> associations,
            IDictionary<long, string> customerRegions,
            IDictionary<long, int> orderCounts,
            IDictionary<long, int> associationCounts,
            IDictionary<long, int> transactionCounts)
        {
            var associatedCustomerIds = associations.Select(x => x.CustomerId).Distinct().ToHashSet();
            var orderedCustomers = customerUsers
                .OrderByDescending(x => associatedCustomerIds.Contains(x.Id))
                .ThenByDescending(x => x.CreatedOn)
                .Take(10)
                .ToList();

            var rows = orderedCustomers.Select(user => new AdminDashboardCustomerActivityVm
            {
                CustomerCode = $"CUST-{user.Id:0000}",
                CustomerName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "N/A" : user.FullName,
                Region = customerRegions.TryGetValue(user.Id, out var region) ? region : "N/A",
                Orders = orderCounts.TryGetValue(user.Id, out var orders) ? orders : 0,
                SupportStaff = associationCounts.TryGetValue(user.Id, out var supportStaff) ? supportStaff : 0,
                Transactions = transactionCounts.TryGetValue(user.Id, out var transactions) ? transactions : 0,
                VerificationStatus = IsVerified(user) ? "Verified" : "Pending"
            }).ToList();

            while (rows.Count < 3)
            {
                rows.Add(new AdminDashboardCustomerActivityVm
                {
                    CustomerCode = "N/A",
                    CustomerName = "N/A",
                    Region = "N/A",
                    VerificationStatus = "Pending"
                });
            }

            return rows;
        }

        private async Task<IDictionary<long, string>> GetCustomerRegions(IList<long> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new Dictionary<long, string>();
            }

            var addresses = await _userAddressRepository.Query()
                .Include(x => x.Address)
                    .ThenInclude(x => x.District)
                .Include(x => x.Address)
                    .ThenInclude(x => x.StateOrProvince)
                .Where(x => customerIds.Contains(x.UserId))
                .OrderByDescending(x => x.LastUsedOn)
                .ToListAsync();

            return customerIds.ToDictionary(
                x => x,
                x =>
                {
                    var address = addresses.FirstOrDefault(a => a.UserId == x)?.Address;
                    return address?.District?.Name
                        ?? address?.StateOrProvince?.Name
                        ?? address?.City
                        ?? "N/A";
                });
        }

        private async Task<IList<VendorCustomerAssociation>> GetAssociations(IList<long> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new List<VendorCustomerAssociation>();
            }

            return await _vendorCustomerAssociationRepository.Query()
                .Include(x => x.Customer)
                .Where(x => customerIds.Contains(x.CustomerId) && !x.IsDeleted && x.IsActive)
                .ToListAsync();
        }

        private async Task<IDictionary<long, int>> GetAssociationCountsByCustomer(IList<long> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new Dictionary<long, int>();
            }

            return await _vendorCustomerAssociationRepository.Query()
                .Where(x => customerIds.Contains(x.CustomerId) && !x.IsDeleted && x.IsActive)
                .GroupBy(x => x.CustomerId)
                .Select(x => new { CustomerId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.CustomerId, x => x.Count);
        }

        private async Task<IDictionary<long, int>> GetTransactionCountsByCustomer(IList<long> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new Dictionary<long, int>();
            }

            return await _customerSettlementRepository.Query()
                .Where(x => customerIds.Contains(x.CustomerId) && !x.IsDeleted)
                .GroupBy(x => x.CustomerId)
                .Select(x => new { CustomerId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.CustomerId, x => x.Count);
        }

        private async Task<IList<AdminDashboardListItemVm>> GetOrdersByRegion()
        {
            var rows = await ReadOrdersByRegion();
            return EnsureRows(rows, DefaultOrderRegions, "0");
        }

        private async Task<IDictionary<long, int>> GetOrderCountsByCustomer(IList<long> customerIds)
        {
            if (customerIds.Count == 0)
            {
                return new Dictionary<long, int>();
            }

            try
            {
                var result = new Dictionary<long, int>();
                var connection = _dbContext.Database.GetDbConnection();
                var shouldClose = connection.State != ConnectionState.Open;
                if (shouldClose)
                {
                    await connection.OpenAsync();
                }

                await using var command = connection.CreateCommand();
                command.CommandText = BuildOrderCountSql(customerIds.Count);
                for (var i = 0; i < customerIds.Count; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@p{i}";
                    parameter.Value = customerIds[i];
                    command.Parameters.Add(parameter);
                }

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result[reader.GetInt64(0)] = Convert.ToInt32(reader.GetInt64(1));
                }

                if (shouldClose)
                {
                    await connection.CloseAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load admin dashboard order counts.");
                return new Dictionary<long, int>();
            }
        }

        private async Task<IList<AdminDashboardListItemVm>> ReadOrdersByRegion()
        {
            try
            {
                var rows = new List<AdminDashboardListItemVm>();
                var connection = _dbContext.Database.GetDbConnection();
                var shouldClose = connection.State != ConnectionState.Open;
                if (shouldClose)
                {
                    await connection.OpenAsync();
                }

                await using var command = connection.CreateCommand();
                command.CommandText = $@"
SELECT TOP 4 COALESCE(d.[Name], sp.[Name], oa.[City], 'N/A') AS [RegionName], COUNT_BIG(1) AS [OrderCount]
FROM [Orders_Order] o
LEFT JOIN [Orders_OrderAddress] oa ON o.[ShippingAddressId] = oa.[Id]
LEFT JOIN [Core_District] d ON oa.[DistrictId] = d.[Id]
LEFT JOIN [Core_StateOrProvince] sp ON oa.[StateOrProvinceId] = sp.[Id]
GROUP BY COALESCE(d.[Name], sp.[Name], oa.[City], 'N/A')
ORDER BY COUNT_BIG(1) DESC";

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new AdminDashboardListItemVm
                    {
                        Label = reader.IsDBNull(0) ? "N/A" : reader.GetString(0),
                        Value = Convert.ToInt64(reader.GetValue(1)).ToString()
                    });
                }

                if (shouldClose)
                {
                    await connection.CloseAsync();
                }

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load admin dashboard orders by region.");
                return new List<AdminDashboardListItemVm>();
            }
        }

        private static string BuildOrderCountSql(int parameterCount)
        {
            var parameters = string.Join(", ", Enumerable.Range(0, parameterCount).Select(x => $"@p{x}"));
            return $@"
SELECT [CustomerId], COUNT_BIG(1)
FROM [Orders_Order]
WHERE [CustomerId] IN ({parameters})
GROUP BY [CustomerId]";
        }

        private static IList<AdminDashboardListItemVm> EnsureRows(
            IList<AdminDashboardListItemVm> rows,
            IEnumerable<string> defaults,
            string defaultValue)
        {
            var result = rows.Take(4).ToList();
            foreach (var label in defaults)
            {
                if (result.Count >= 4)
                {
                    break;
                }

                if (result.Any(x => string.Equals(x.Label, label, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(new AdminDashboardListItemVm { Label = label, Value = defaultValue });
            }

            return result;
        }
    }
}
