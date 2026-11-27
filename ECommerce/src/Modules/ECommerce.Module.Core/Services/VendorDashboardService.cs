using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Module.Core.Services
{
    public class VendorDashboardService : IVendorDashboardService
    {
        private const int LowStockThreshold = 10;
        private const int CreditDueDays = 30;

        private readonly IRepository<VendorProduct> _vendorProductRepository;
        private readonly IRepository<VendorOffer> _vendorOfferRepository;
        private readonly IRepository<VendorCustomerAssociation> _vendorCustomerAssociationRepository;
        private readonly IRepository<CustomerSettlement> _customerSettlementRepository;
        private readonly IOrderService _orderService;
        private readonly ICurrencyService _currencyService;

        public VendorDashboardService(
            IRepository<VendorProduct> vendorProductRepository,
            IRepository<VendorOffer> vendorOfferRepository,
            IRepository<VendorCustomerAssociation> vendorCustomerAssociationRepository,
            IRepository<CustomerSettlement> customerSettlementRepository,
            IOrderService orderService,
            ICurrencyService currencyService
        )
        {
            _vendorProductRepository = vendorProductRepository;
            _vendorOfferRepository = vendorOfferRepository;
            _vendorCustomerAssociationRepository = vendorCustomerAssociationRepository;
            _customerSettlementRepository = customerSettlementRepository;
            _orderService = orderService;
            _currencyService = currencyService;
        }

        public async Task<VendorDashboardVm> GetDashboard(long vendorId)
        {
            return new VendorDashboardVm
            {
                Home = await GetHome(vendorId),
                Inventory = await GetInventory(vendorId),
                Sales = await GetSales(vendorId),
                Credit = await GetCredit(vendorId),
                Fees = await GetFees(vendorId),
                Reports = await GetReports(vendorId),
                Offers = await GetOffers(vendorId),
            };
        }

        public async Task<VendorDashboardHomeVm> GetHome(long vendorId)
        {
            var inventory = await GetInventory(vendorId);
            var orders = await GetVendorOrderLines(vendorId);
            var orderRows = DistinctOrders(orders);
            var creditSummary = await BuildCreditSummary(vendorId, orders);

            var totalRevenue = orderRows.Sum(x => x.OrderAmount);
            var pendingOrders = orderRows
                .Where(x => IsPendingOrder(x.OrderStatus))
                .OrderByDescending(x => x.OrderedDate)
                .Take(5)
                .Select(x => new VendorPendingOrderVm
                {
                    OrderId = x.OrderId,
                    Buyer = Display(x.CustomerName),
                    Days = Math.Max(0, (DateTime.UtcNow.Date - x.OrderedDate.Date).Days),
                    Amount = x.OrderAmount,
                    AmountString = Money(x.OrderAmount),
                })
                .ToList();

            return new VendorDashboardHomeVm
            {
                TotalAgroRevenue = totalRevenue,
                TotalAgroRevenueString = Money(totalRevenue),
                TotalFarmOrders = orderRows.Count,
                Products = inventory.TotalProducts,
                PendingDispatches = pendingOrders.Count,
                MonthlyProductRevenue = BuildMonthlyAmounts(orderRows, x => x.OrderAmount),
                LowStockProducts = inventory
                    .Products.Where(x => x.Stock <= LowStockThreshold)
                    .Take(4)
                    .Select(x => new VendorLowStockProductVm
                    {
                        VendorProductId = x.VendorProductId,
                        ProductName = x.ProductName,
                        Stock = x.Stock,
                        ExpiryDate = x.ExpiryDate,
                        Status = x.Stock <= 0 ? "Out of stock" : "Low stock",
                    })
                    .ToList(),
                RecentFarmOrders = orderRows
                    .OrderByDescending(x => x.OrderedDate)
                    .Take(5)
                    .Select(x => new VendorRecentOrderVm
                    {
                        OrderId = x.OrderId,
                        Buyer = Display(x.CustomerName),
                        Status = Display(x.OrderStatus),
                        Amount = x.OrderAmount,
                        AmountString = Money(x.OrderAmount),
                        OrderedDate = x.OrderedDate,
                    })
                    .ToList(),
                PendingOrders = pendingOrders,
                DispatchStatus = BuildDispatchStatus(orderRows),
                CreditBuyers = creditSummary.BuyerSummary,
                CreditSaleMatrix = creditSummary.SaleMatrix,
                CreditSettlement = creditSummary.Settlement,
                SalesReturnMatrix = new VendorSalesReturnMatrixVm { ReturnValueString = Money(0) },
            };
        }

        public async Task<VendorInventoryDashboardVm> GetInventory(long vendorId)
        {
            var products = await _vendorProductRepository
                .Query()
                .Where(x => x.VendorId == vendorId && x.IsActive)
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            var productRows = products
                .Select(x =>
                {
                    var price = GetProductPrice(x);
                    return new VendorInventoryProductVm
                    {
                        VendorProductId = x.Id,
                        ProductId = x.ProductId,
                        ProductName = Display(x.ProductName),
                        Price = price,
                        PriceString = Money(price),
                        Stock = x.StockQuantity,
                        Status = GetStockStatus(x.StockQuantity),
                        BatchNumber = x.BatchNumber,
                        ExpiryDate = x.ExpiryDate,
                    };
                })
                .ToList();

            return new VendorInventoryDashboardVm
            {
                TotalProducts = products.Count,
                LowStock = products.Count(x =>
                    x.StockQuantity > 0 && x.StockQuantity <= LowStockThreshold
                ),
                StockOut = products.Count(x => x.StockQuantity <= 0),
                ExpiringSoon30Days = products.Count(x =>
                    x.ExpiryDate.HasValue
                    && x.ExpiryDate.Value.Date >= DateTime.UtcNow.Date
                    && x.ExpiryDate.Value.Date <= DateTime.UtcNow.Date.AddDays(30)
                ),
                Products = productRows,
            };
        }

        public async Task<VendorSalesDashboardVm> GetSales(long vendorId)
        {
            var orders = await GetVendorOrderLines(vendorId);
            var orderRows = DistinctOrders(orders);
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var associations = await GetAssociations(vendorId);
            var settlements = await GetSettlements(vendorId);
            var creditSummary = BuildCreditClientStatus(associations, orderRows, settlements);

            var monthlyCashOrders = orderRows
                .Where(x =>
                    IsCashOrder(x)
                    && x.OrderedDate.Month == currentMonth
                    && x.OrderedDate.Year == currentYear
                )
                .ToList();
            var monthlyCreditOrders = orderRows
                .Where(x =>
                    IsCreditOrder(x)
                    && x.OrderedDate.Month == currentMonth
                    && x.OrderedDate.Year == currentYear
                )
                .ToList();

            return new VendorSalesDashboardVm
            {
                CashSalesMonthly = monthlyCashOrders.Sum(x => x.OrderAmount),
                CashSalesMonthlyString = Money(monthlyCashOrders.Sum(x => x.OrderAmount)),
                CashDiscountApplied = 0,
                CashDiscountAppliedString = Money(0),
                CreditSalesMonthly = monthlyCreditOrders.Sum(x => x.OrderAmount),
                CreditSalesMonthlyString = Money(monthlyCreditOrders.Sum(x => x.OrderAmount)),
                OutstandingCredit = creditSummary.OutstandingCredit,
                OutstandingCreditString = Money(creditSummary.OutstandingCredit),
                InvoicesGenerated = orderRows.Count,
                CashSalesByMonth = BuildMonthlyAmounts(
                    orderRows.Where(IsCashOrder),
                    x => x.OrderAmount
                ),
                CreditSalesByMonth = BuildMonthlyAmounts(
                    orderRows.Where(IsCreditOrder),
                    x => x.OrderAmount
                ),
                CreditClientStatus = creditSummary.Status,
                CreditClientTracking = creditSummary.Tracking,
            };
        }

        public async Task<VendorCreditDashboardVm> GetCredit(long vendorId)
        {
            var orders = DistinctOrders(await GetVendorOrderLines(vendorId));
            var associations = await GetAssociations(vendorId);
            var settlements = await GetSettlements(vendorId);
            var settlementByCustomer = settlements
                .GroupBy(x => x.CustomerId)
                .ToDictionary(x => x.Key, x => x.Sum(s => s.AmountPaid));

            return new VendorCreditDashboardVm
            {
                CreditClients = associations
                    .Select(x => new VendorCreditClientCardVm
                    {
                        CustomerId = x.CustomerId,
                        CustomerName = Display(x.CustomerName),
                        VendorCode = $"V{vendorId}-C{x.CustomerId}",
                        CreditLimit = x.MaxCreditAmount,
                        CreditLimitString = Money(x.MaxCreditAmount),
                        From = x.StartDate,
                        Till = null,
                        Status = x.IsActive ? "Active" : "Inactive",
                    })
                    .ToList(),
                BillingStatements = orders
                    .Where(IsCreditOrder)
                    .OrderByDescending(x => x.OrderedDate)
                    .Take(10)
                    .Select(x =>
                    {
                        settlementByCustomer.TryGetValue(x.CustomerId, out var settled);
                        return new VendorBillingStatementVm
                        {
                            CustomerId = x.CustomerId,
                            Buyer = Display(x.CustomerName),
                            Invoice = x.OrderId,
                            InvoiceDate = x.OrderedDate,
                            DueDate = x.OrderedDate.AddDays(CreditDueDays),
                            Amount = x.OrderAmount,
                            AmountString = Money(x.OrderAmount),
                            Status = GetBillingStatus(x, settled),
                            Settlement = settled,
                            SettlementString = Money(settled),
                            Actions = new List<string> { "PDF", "WhatsApp", "SMS" },
                        };
                    })
                    .ToList(),
                PaymentReminderRules = GetReminderRules(),
                SettlementDiscountControls = new VendorSettlementDiscountControlsVm
                {
                    DiscountEnabled = false,
                    DiscountPercentage = 0,
                    ValidityWindowDays = 0,
                    AutoDeductionEnabled = true,
                },
            };
        }

        public async Task<VendorFeeDashboardVm> GetFees(long vendorId)
        {
            var orderRows = DistinctOrders(await GetVendorOrderLines(vendorId));
            var feeRows = orderRows
                .Select(x => new VendorOrderFeeVm
                {
                    OrderId = x.OrderId,
                    PaymentType = Display(x.PaymentMethod),
                    OrderAmount = x.OrderAmount,
                    OrderAmountString = Money(x.OrderAmount),
                    FeeRate = 0,
                    FeeCharged = 0,
                    FeeChargedString = Money(0),
                })
                .ToList();

            return new VendorFeeDashboardVm
            {
                CashSaleFeeRate = 0,
                CreditSaleFeeRate = 0,
                TotalFeeAccumulated = feeRows.Sum(x => x.FeeCharged),
                TotalFeeAccumulatedString = Money(feeRows.Sum(x => x.FeeCharged)),
                FeeSettledThisWeek = 0,
                FeeSettledThisWeekString = Money(0),
                FeeChargedPerOrder = feeRows.Take(20).ToList(),
                WeeklyFeeStatements = BuildWeeklyFeeStatements(feeRows),
                WeeklyFeeSettlementNote =
                    "Platform fees are applied per order based on payment type and are automatically settled weekly against vendor payouts.",
            };
        }

        public async Task<VendorReportsDashboardVm> GetReports(long vendorId)
        {
            var orders = await GetVendorOrderLines(vendorId);
            var orderRows = DistinctOrders(orders);
            var creditRows = orderRows.Where(IsCreditOrder).ToList();
            var productPerformance = orders
                .GroupBy(x => Display(x.ItemName))
                .Select(x => new VendorProductPerformanceVm
                {
                    ProductName = x.Key,
                    Revenue = x.Sum(o => o.ItemAmount),
                    RevenueString = Money(x.Sum(o => o.ItemAmount)),
                    QuantitySold = x.Sum(o => o.ItemQuantity),
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            var totalGst = orders.Sum(x => x.ItemTaxAmount);
            var taxableAmount = orders.Sum(x => x.ItemAmount) - totalGst;

            return new VendorReportsDashboardVm
            {
                SalesReport = new VendorSalesReportVm
                {
                    TotalSales = orderRows.Sum(x => x.OrderAmount),
                    TotalSalesString = Money(orderRows.Sum(x => x.OrderAmount)),
                    TotalOrders = orderRows.Count,
                    RegionsCovered = 0,
                },
                CreditAgeing = new VendorCreditAgeingVm
                {
                    Days0To30 = creditRows.Where(x => AgeInDays(x) <= 30).Sum(x => x.OrderAmount),
                    Days31To60 = creditRows
                        .Where(x => AgeInDays(x) >= 31 && AgeInDays(x) <= 60)
                        .Sum(x => x.OrderAmount),
                    Days61To90 = creditRows
                        .Where(x => AgeInDays(x) >= 61 && AgeInDays(x) <= 90)
                        .Sum(x => x.OrderAmount),
                },
                TaxReport = new VendorTaxReportVm
                {
                    TaxableAmount = taxableAmount,
                    TaxableAmountString = Money(taxableAmount),
                    TotalGst = totalGst,
                    TotalGstString = Money(totalGst),
                    Cgst = totalGst / 2,
                    CgstString = Money(totalGst / 2),
                    Sgst = totalGst / 2,
                    SgstString = Money(totalGst / 2),
                },
                ProductPerformance = productPerformance,
                DeliveryPerformance = new VendorDeliveryPerformanceVm
                {
                    TotalDeliveries = orderRows.Count(x => IsDelivered(x.OrderStatus)),
                    OnTime = orderRows.Count(x => IsDelivered(x.OrderStatus)),
                    Delayed = orderRows.Count(IsDelayed),
                    AverageDeliveryTimeDays = 0,
                },
            };
        }

        public async Task<VendorOffersDashboardVm> GetOffers(long vendorId)
        {
            var activeOffers = await GetVendorOffers(vendorId, includeInactive: false);
            var activeByType = activeOffers
                .GroupBy(x => NormalizeOfferType(x.OfferType))
                .ToDictionary(x => x.Key, x => x.Count());

            return new VendorOffersDashboardVm
            {
                VendorSpecificOffers = GetOfferTypes()
                    .Select(x =>
                    {
                        activeByType.TryGetValue(x.Code, out var count);
                        x.ActiveOfferCount = count;
                        return x;
                    })
                    .ToList(),
                ActiveVendorOffers = activeOffers,
                PlatformOffers = GetPlatformOffers(vendorId),
            };
        }

        public async Task<IList<VendorOfferDetailVm>> GetVendorOffers(
            long vendorId,
            string offerType = null,
            bool includeInactive = false
        )
        {
            var normalizedType = NormalizeOfferType(offerType);
            var query = _vendorOfferRepository
                .Query()
                .Where(x => x.VendorId == vendorId && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(normalizedType))
            {
                query = query.Where(x => x.OfferType == normalizedType);
            }

            if (!includeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            var offers = await query.OrderByDescending(x => x.LatestUpdatedOn).ToListAsync();

            return offers.Select(MapOffer).ToList();
        }

        public async Task<VendorOfferDetailVm> GetVendorOffer(long vendorId, long offerId)
        {
            var offer = await GetOfferEntity(vendorId, offerId);
            return MapOffer(offer);
        }

        public async Task<VendorOfferDetailVm> CreateVendorOffer(
            long vendorId,
            VendorOfferFormVm model,
            long currentUserId
        )
        {
            ValidateOfferForm(model);
            var now = DateTimeOffset.UtcNow;
            var offer = new VendorOffer
            {
                VendorId = vendorId,
                OfferType = NormalizeOfferType(model.OfferType),
                Title = model.Title?.Trim(),
                Description = model.Description,
                TermsAndConditions = model.TermsAndConditions,
                DiscountType = NormalizeDiscountType(model.DiscountType),
                DiscountValue = model.DiscountValue,
                MaxDiscountAmount = model.MaxDiscountAmount,
                MinimumOrderAmount = model.MinimumOrderAmount,
                MinimumQuantity = model.MinimumQuantity,
                CouponCode = model.CouponCode?.Trim(),
                StartOn = model.StartOn,
                EndOn = model.EndOn,
                IsActive = model.IsActive,
                CreatedOn = now,
                CreatedById = currentUserId,
                LatestUpdatedOn = now,
                LatestUpdatedById = currentUserId,
                IsDeleted = false,
            };

            _vendorOfferRepository.Add(offer);
            await _vendorOfferRepository.SaveChangesAsync();
            return MapOffer(offer);
        }

        public async Task<VendorOfferDetailVm> UpdateVendorOffer(
            long vendorId,
            long offerId,
            VendorOfferFormVm model,
            long currentUserId
        )
        {
            ValidateOfferForm(model);
            var offer = await GetOfferEntity(vendorId, offerId);
            offer.OfferType = NormalizeOfferType(model.OfferType);
            offer.Title = model.Title?.Trim();
            offer.Description = model.Description;
            offer.TermsAndConditions = model.TermsAndConditions;
            offer.DiscountType = NormalizeDiscountType(model.DiscountType);
            offer.DiscountValue = model.DiscountValue;
            offer.MaxDiscountAmount = model.MaxDiscountAmount;
            offer.MinimumOrderAmount = model.MinimumOrderAmount;
            offer.MinimumQuantity = model.MinimumQuantity;
            offer.CouponCode = model.CouponCode?.Trim();
            offer.StartOn = model.StartOn;
            offer.EndOn = model.EndOn;
            offer.IsActive = model.IsActive;
            offer.LatestUpdatedOn = DateTimeOffset.UtcNow;
            offer.LatestUpdatedById = currentUserId;

            await _vendorOfferRepository.SaveChangesAsync();
            return MapOffer(offer);
        }

        public async Task DeleteVendorOffer(long vendorId, long offerId, long currentUserId)
        {
            var offer = await GetOfferEntity(vendorId, offerId);
            offer.IsDeleted = true;
            offer.IsActive = false;
            offer.LatestUpdatedOn = DateTimeOffset.UtcNow;
            offer.LatestUpdatedById = currentUserId;
            await _vendorOfferRepository.SaveChangesAsync();
        }

        public async Task<VendorOfferDetailVm> SetVendorOfferStatus(
            long vendorId,
            long offerId,
            bool isActive,
            long currentUserId
        )
        {
            var offer = await GetOfferEntity(vendorId, offerId);
            offer.IsActive = isActive;
            offer.LatestUpdatedOn = DateTimeOffset.UtcNow;
            offer.LatestUpdatedById = currentUserId;
            await _vendorOfferRepository.SaveChangesAsync();
            return MapOffer(offer);
        }

        public IList<PlatformOfferDetailVm> GetPlatformOffers(long vendorId)
        {
            return BuildPlatformOffers(false);
        }

        public PlatformOfferDetailVm GetPlatformOffer(long vendorId, string code)
        {
            var normalizedCode = NormalizeOfferType(code);
            return BuildPlatformOffers(false)
                .FirstOrDefault(x => NormalizeOfferType(x.Code) == normalizedCode);
        }

        private async Task<VendorOffer> GetOfferEntity(long vendorId, long offerId)
        {
            var offer = await _vendorOfferRepository
                .Query()
                .FirstOrDefaultAsync(x =>
                    x.Id == offerId && x.VendorId == vendorId && !x.IsDeleted
                );

            if (offer == null)
            {
                throw new InvalidOperationException("Vendor offer not found");
            }

            return offer;
        }

        private VendorOfferDetailVm MapOffer(VendorOffer offer)
        {
            return new VendorOfferDetailVm
            {
                Id = offer.Id,
                VendorId = offer.VendorId,
                OfferType = offer.OfferType,
                OfferTypeName = GetOfferTypeName(offer.OfferType),
                Title = offer.Title,
                Description = offer.Description,
                TermsAndConditions = offer.TermsAndConditions,
                DiscountType = offer.DiscountType,
                DiscountValue = offer.DiscountValue,
                MaxDiscountAmount = offer.MaxDiscountAmount,
                MaxDiscountAmountString = offer.MaxDiscountAmount.HasValue
                    ? Money(offer.MaxDiscountAmount.Value)
                    : null,
                MinimumOrderAmount = offer.MinimumOrderAmount,
                MinimumOrderAmountString = offer.MinimumOrderAmount.HasValue
                    ? Money(offer.MinimumOrderAmount.Value)
                    : null,
                MinimumQuantity = offer.MinimumQuantity,
                CouponCode = offer.CouponCode,
                StartOn = offer.StartOn,
                EndOn = offer.EndOn,
                IsActive = offer.IsActive,
                Status = GetOfferStatus(offer),
                CreatedOn = offer.CreatedOn,
                LatestUpdatedOn = offer.LatestUpdatedOn,
            };
        }

        private static void ValidateOfferForm(VendorOfferFormVm model)
        {
            if (model == null)
            {
                throw new ArgumentException("Offer payload is required");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                throw new ArgumentException("Offer title is required");
            }

            if (string.IsNullOrWhiteSpace(model.OfferType))
            {
                throw new ArgumentException("Offer type is required");
            }

            if (!GetOfferTypeCodes().Contains(NormalizeOfferType(model.OfferType)))
            {
                throw new ArgumentException("Invalid offer type");
            }

            if (model.DiscountValue < 0)
            {
                throw new ArgumentException("Discount value cannot be negative");
            }

            if (model.StartOn.HasValue && model.EndOn.HasValue && model.EndOn < model.StartOn)
            {
                throw new ArgumentException("Offer end date cannot be before start date");
            }
        }

        private static IList<VendorOfferTypeVm> GetOfferTypes()
        {
            return new List<VendorOfferTypeVm>
            {
                new VendorOfferTypeVm
                {
                    Code = "seasonal-discount",
                    Name = "Seasonal Discounts",
                    Description =
                        "Offer limited-time discounts during festivals or seasonal demand peaks.",
                },
                new VendorOfferTypeVm
                {
                    Code = "volume-based",
                    Name = "Volume-Based Schemes",
                    Description = "Encourage bulk purchases with quantity-based pricing benefits.",
                },
                new VendorOfferTypeVm
                {
                    Code = "cash-only",
                    Name = "Cash-Only Sales Offers",
                    Description = "Provide special pricing for customers paying with cash.",
                },
                new VendorOfferTypeVm
                {
                    Code = "loyalty",
                    Name = "Loyalty Offers",
                    Description = "Reward repeat customers with exclusive discounts and benefits.",
                },
            };
        }

        private static IList<string> GetOfferTypeCodes()
        {
            return GetOfferTypes().Select(x => x.Code).ToList();
        }

        private static string GetOfferTypeName(string offerType)
        {
            var type = GetOfferTypes().FirstOrDefault(x => x.Code == NormalizeOfferType(offerType));
            return type?.Name ?? Display(offerType);
        }

        private static IList<PlatformOfferDetailVm> BuildPlatformOffers(bool isParticipating)
        {
            return new List<PlatformOfferDetailVm>
            {
                new PlatformOfferDetailVm
                {
                    Code = "ziraat-wide-promotions",
                    Title = "Ziraat-Wide Promotional Campaigns",
                    Description = "View and track ongoing Ziraat-wide promotional campaigns.",
                    Benefits = new List<string>
                    {
                        "Platform-level visibility",
                        "Campaign placement support",
                        "Shared promotional tracking",
                    },
                    Eligibility = new List<string>
                    {
                        "Active vendor account",
                        "At least one active assigned product",
                    },
                    Actions = new List<string> { "View Campaign", "Track Performance" },
                    IsParticipating = isParticipating,
                },
                new PlatformOfferDetailVm
                {
                    Code = "co-funded-offers",
                    Title = "Co-Funded Offers",
                    Description =
                        "Participate in co-funded offers to increase visibility and sales.",
                    Benefits = new List<string>
                    {
                        "Shared discount burden",
                        "Higher product visibility",
                        "Sales uplift tracking",
                    },
                    Eligibility = new List<string>
                    {
                        "Active vendor account",
                        "Eligible products with available stock",
                    },
                    Actions = new List<string> { "Request Participation", "View Terms" },
                    IsParticipating = isParticipating,
                },
            };
        }

        private static string GetOfferStatus(VendorOffer offer)
        {
            if (!offer.IsActive)
            {
                return "Inactive";
            }

            var now = DateTimeOffset.UtcNow;
            if (offer.StartOn.HasValue && offer.StartOn.Value > now)
            {
                return "Scheduled";
            }

            if (offer.EndOn.HasValue && offer.EndOn.Value < now)
            {
                return "Expired";
            }

            return "Active";
        }

        private static string NormalizeOfferType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        }

        private static string NormalizeDiscountType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "percentage"
                : value.Trim().ToLowerInvariant();
        }

        private async Task<List<OrderDto>> GetVendorOrderLines(long vendorId)
        {
            if (_orderService == null)
            {
                return new List<OrderDto>();
            }

            return await _orderService.GetVendorOrders(vendorId);
        }

        private async Task<List<VendorCustomerAssociation>> GetAssociations(long vendorId)
        {
            return await _vendorCustomerAssociationRepository
                .Query()
                .Where(x => x.VendorId == vendorId && !x.IsDeleted)
                .OrderBy(x => x.CustomerName)
                .ToListAsync();
        }

        private async Task<List<CustomerSettlement>> GetSettlements(long vendorId)
        {
            return await _customerSettlementRepository
                .Query()
                .Where(x => x.VendorId == vendorId && !x.IsDeleted)
                .ToListAsync();
        }

        private async Task<(
            VendorCreditBuyerSummaryVm BuyerSummary,
            VendorCreditSaleMatrixVm SaleMatrix,
            VendorCreditSettlementVm Settlement
        )> BuildCreditSummary(long vendorId, List<OrderDto> orders)
        {
            var associations = await GetAssociations(vendorId);
            var settlements = await GetSettlements(vendorId);
            var orderRows = DistinctOrders(orders);
            var creditSales = orderRows.Where(IsCreditOrder).Sum(x => x.OrderAmount);
            var settled = settlements.Sum(x => x.AmountPaid);
            var pendingCredit = Math.Max(0, creditSales - settled);

            return (
                new VendorCreditBuyerSummaryVm
                {
                    TotalBuyersOnCredit = associations.Count(x => x.AllowCredit),
                    Buyers = associations
                        .Where(x => x.AllowCredit)
                        .Take(3)
                        .Select(x => new VendorCreditBuyerVm
                        {
                            CustomerId = x.CustomerId,
                            CustomerName = Display(x.CustomerName),
                            Credit = x.MaxCreditAmount,
                            CreditString = Money(x.MaxCreditAmount),
                            Status = x.IsActive ? "Active" : "Inactive",
                        })
                        .ToList(),
                },
                new VendorCreditSaleMatrixVm
                {
                    TotalCreditSales = creditSales,
                    TotalCreditSalesString = Money(creditSales),
                    PendingCredit = pendingCredit,
                    PendingCreditString = Money(pendingCredit),
                    OverdueCredit = CalculateOverdueCredit(orderRows, settlements),
                    OverdueCreditString = Money(CalculateOverdueCredit(orderRows, settlements)),
                },
                new VendorCreditSettlementVm
                {
                    TotalSettledCredit = settled,
                    TotalSettledCreditString = Money(settled),
                }
            );
        }

        private (
            VendorCreditClientStatusVm Status,
            IList<VendorCreditClientTrackingVm> Tracking,
            decimal OutstandingCredit
        ) BuildCreditClientStatus(
            List<VendorCustomerAssociation> associations,
            List<OrderDto> orderRows,
            List<CustomerSettlement> settlements
        )
        {
            var settlementByCustomer = settlements
                .GroupBy(x => x.CustomerId)
                .ToDictionary(x => x.Key, x => x.Sum(s => s.AmountPaid));
            var tracking = associations
                .Where(x => x.AllowCredit)
                .Select(association =>
                {
                    var customerCreditOrders = orderRows
                        .Where(o => o.CustomerId == association.CustomerId && IsCreditOrder(o))
                        .ToList();
                    settlementByCustomer.TryGetValue(association.CustomerId, out var settled);
                    var outstanding = Math.Max(
                        0,
                        customerCreditOrders.Sum(o => o.OrderAmount) - settled
                    );
                    var overdue = customerCreditOrders.Any(o => AgeInDays(o) > CreditDueDays)
                        ? outstanding
                        : 0;

                    return new VendorCreditClientTrackingVm
                    {
                        CustomerId = association.CustomerId,
                        Client = Display(association.CustomerName),
                        CreditLimit = association.MaxCreditAmount,
                        CreditLimitString = Money(association.MaxCreditAmount),
                        Outstanding = outstanding,
                        OutstandingString = Money(outstanding),
                        Overdue = overdue,
                        OverdueString = Money(overdue),
                        Reminders = overdue > 0 ? 1 : 0,
                    };
                })
                .ToList();

            return (
                new VendorCreditClientStatusVm
                {
                    WithinLimit = tracking.Count(x =>
                        x.CreditLimit <= 0 || x.Outstanding < x.CreditLimit * 0.8m
                    ),
                    NearLimit = tracking.Count(x =>
                        x.CreditLimit > 0
                        && x.Outstanding >= x.CreditLimit * 0.8m
                        && x.Outstanding <= x.CreditLimit
                    ),
                    OverLimit = tracking.Count(x =>
                        x.CreditLimit > 0 && x.Outstanding > x.CreditLimit
                    ),
                },
                tracking,
                tracking.Sum(x => x.Outstanding)
            );
        }

        private VendorDispatchStatusVm BuildDispatchStatus(List<OrderDto> orderRows)
        {
            return new VendorDispatchStatusVm
            {
                Processing = orderRows.Count(x => IsProcessing(x.OrderStatus)),
                ReadyToShip = orderRows.Count(x => IsReadyToShip(x.OrderStatus)),
                Delayed = orderRows.Count(IsDelayed),
            };
        }

        private IList<MonthlyAmountVm> BuildMonthlyAmounts(
            IEnumerable<OrderDto> rows,
            Func<OrderDto, decimal> amountSelector
        )
        {
            var grouped = rows.GroupBy(x => new
                {
                    x.OrderedDate.Year,
                    x.OrderedDate.Month,
                    x.OrderId,
                })
                .Select(x => x.First())
                .GroupBy(x => x.OrderedDate.Month)
                .ToDictionary(x => x.Key, x => x.Sum(amountSelector));

            return Enumerable
                .Range(1, 12)
                .Select(month =>
                {
                    grouped.TryGetValue(month, out var amount);
                    return new MonthlyAmountVm
                    {
                        Month = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[
                            month - 1
                        ],
                        MonthNumber = month,
                        Amount = amount,
                        AmountString = Money(amount),
                    };
                })
                .ToList();
        }

        private IList<VendorWeeklyFeeStatementVm> BuildWeeklyFeeStatements(
            IList<VendorOrderFeeVm> feeRows
        )
        {
            return feeRows
                .GroupBy(x => GetWeekLabel(DateTime.UtcNow))
                .Select(x => new VendorWeeklyFeeStatementVm
                {
                    Week = x.Key,
                    Orders = x.Count(),
                    TotalFee = x.Sum(f => f.FeeCharged),
                    TotalFeeString = Money(x.Sum(f => f.FeeCharged)),
                })
                .DefaultIfEmpty(
                    new VendorWeeklyFeeStatementVm
                    {
                        Week = GetWeekLabel(DateTime.UtcNow),
                        Orders = 0,
                        TotalFee = 0,
                        TotalFeeString = Money(0),
                    }
                )
                .ToList();
        }

        private static List<OrderDto> DistinctOrders(IEnumerable<OrderDto> rows)
        {
            return rows.GroupBy(x => x.OrderId)
                .Select(x => x.First())
                .OrderByDescending(x => x.OrderedDate)
                .ToList();
        }

        private static decimal GetProductPrice(VendorProduct product)
        {
            return product.DiscountPrice
                ?? product.AssignedPrice
                ?? product.MaximumRetailPrice
                ?? product.OriginalPrice
                ?? 0;
        }

        private static string GetStockStatus(int stock)
        {
            if (stock <= 0)
            {
                return "Stock Out";
            }

            if (stock <= LowStockThreshold)
            {
                return "Low Stock";
            }

            return "Available";
        }

        private static bool IsCreditOrder(OrderDto order)
        {
            return string.Equals(
                order.PaymentMethod,
                "BuyOnCredit",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static bool IsCashOrder(OrderDto order)
        {
            return !IsCreditOrder(order);
        }

        private static bool IsPendingOrder(string status)
        {
            return IsProcessing(status) || IsReadyToShip(status);
        }

        private static bool IsProcessing(string status)
        {
            return MatchesStatus(
                status,
                "New",
                "OnHold",
                "PendingPayment",
                "PaymentReceived",
                "Invoiced"
            );
        }

        private static bool IsReadyToShip(string status)
        {
            return MatchesStatus(status, "Shipping");
        }

        private static bool IsDelivered(string status)
        {
            return MatchesStatus(status, "Shipped", "Complete", "Closed");
        }

        private static bool IsDelayed(OrderDto order)
        {
            return IsPendingOrder(order.OrderStatus) && AgeInDays(order) > 2;
        }

        private static bool MatchesStatus(string status, params string[] statuses)
        {
            return statuses.Any(x => string.Equals(status, x, StringComparison.OrdinalIgnoreCase));
        }

        private static int AgeInDays(OrderDto order)
        {
            return Math.Max(0, (DateTime.UtcNow.Date - order.OrderedDate.Date).Days);
        }

        private static decimal CalculateOverdueCredit(
            List<OrderDto> orderRows,
            List<CustomerSettlement> settlements
        )
        {
            var settled = settlements.Sum(x => x.AmountPaid);
            var overdueCredit = orderRows
                .Where(x => IsCreditOrder(x) && AgeInDays(x) > CreditDueDays)
                .Sum(x => x.OrderAmount);
            return Math.Max(0, overdueCredit - settled);
        }

        private static string GetBillingStatus(OrderDto order, decimal settledAmount)
        {
            if (settledAmount >= order.OrderAmount)
            {
                return "Settled";
            }

            return AgeInDays(order) > CreditDueDays ? "Overdue" : "Pending";
        }

        private static IList<VendorPaymentReminderRuleVm> GetReminderRules()
        {
            return new List<VendorPaymentReminderRuleVm>
            {
                new VendorPaymentReminderRuleVm
                {
                    Name = "PURCHASE DAY",
                    Timing = "Same Day",
                    Channels = new List<string> { "SMS", "WHATSAPP" },
                },
                new VendorPaymentReminderRuleVm
                {
                    Name = "BEFORE DUE DATE",
                    Timing = "Same Day",
                    Channels = new List<string> { "SMS", "WHATSAPP" },
                },
                new VendorPaymentReminderRuleVm
                {
                    Name = "ON DUE DATE",
                    Timing = "Same Day",
                    Channels = new List<string> { "SMS", "WHATSAPP" },
                },
                new VendorPaymentReminderRuleVm
                {
                    Name = "OVERDUE",
                    Timing = "Same Day",
                    PenaltyApplicable = true,
                    Channels = new List<string> { "SMS", "WHATSAPP" },
                },
            };
        }

        private static string Display(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "N/A" : value;
        }

        private static string GetWeekLabel(DateTime date)
        {
            var start = date.Date.AddDays(-(int)date.DayOfWeek);
            var end = start.AddDays(6);
            return $"{start:dd MMM} - {end:dd MMM}";
        }

        private string Money(decimal amount)
        {
            return _currencyService.FormatCurrency(amount);
        }
    }
}
