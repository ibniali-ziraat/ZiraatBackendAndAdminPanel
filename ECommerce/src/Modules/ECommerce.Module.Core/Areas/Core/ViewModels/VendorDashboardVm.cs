using System;
using System.Collections.Generic;

namespace ECommerce.Module.Core.Areas.Core.ViewModels
{
    public class VendorDashboardVm
    {
        public VendorDashboardHomeVm Home { get; set; }

        public VendorInventoryDashboardVm Inventory { get; set; }

        public VendorSalesDashboardVm Sales { get; set; }

        public VendorCreditDashboardVm Credit { get; set; }

        public VendorFeeDashboardVm Fees { get; set; }

        public VendorReportsDashboardVm Reports { get; set; }

        public VendorOffersDashboardVm Offers { get; set; }
    }

    public class VendorDashboardHomeVm
    {
        public decimal TotalAgroRevenue { get; set; }

        public string TotalAgroRevenueString { get; set; }

        public int TotalFarmOrders { get; set; }

        public int Products { get; set; }

        public int PendingDispatches { get; set; }

        public IList<MonthlyAmountVm> MonthlyProductRevenue { get; set; } =
            new List<MonthlyAmountVm>();

        public IList<VendorLowStockProductVm> LowStockProducts { get; set; } =
            new List<VendorLowStockProductVm>();

        public IList<VendorRecentOrderVm> RecentFarmOrders { get; set; } =
            new List<VendorRecentOrderVm>();

        public IList<VendorPendingOrderVm> PendingOrders { get; set; } =
            new List<VendorPendingOrderVm>();

        public VendorDispatchStatusVm DispatchStatus { get; set; } = new VendorDispatchStatusVm();

        public VendorCreditBuyerSummaryVm CreditBuyers { get; set; } =
            new VendorCreditBuyerSummaryVm();

        public VendorCreditSaleMatrixVm CreditSaleMatrix { get; set; } =
            new VendorCreditSaleMatrixVm();

        public VendorCreditSettlementVm CreditSettlement { get; set; } =
            new VendorCreditSettlementVm();

        public VendorSalesReturnMatrixVm SalesReturnMatrix { get; set; } =
            new VendorSalesReturnMatrixVm();
    }

    public class VendorInventoryDashboardVm
    {
        public int TotalProducts { get; set; }

        public int LowStock { get; set; }

        public int StockOut { get; set; }

        public int ExpiringSoon30Days { get; set; }

        public IList<VendorInventoryProductVm> Products { get; set; } =
            new List<VendorInventoryProductVm>();
    }

    public class VendorSalesDashboardVm
    {
        public decimal CashSalesMonthly { get; set; }

        public string CashSalesMonthlyString { get; set; }

        public decimal CashDiscountApplied { get; set; }

        public string CashDiscountAppliedString { get; set; }

        public decimal CreditSalesMonthly { get; set; }

        public string CreditSalesMonthlyString { get; set; }

        public decimal OutstandingCredit { get; set; }

        public string OutstandingCreditString { get; set; }

        public int InvoicesGenerated { get; set; }

        public IList<MonthlyAmountVm> CashSalesByMonth { get; set; } = new List<MonthlyAmountVm>();

        public IList<MonthlyAmountVm> CreditSalesByMonth { get; set; } =
            new List<MonthlyAmountVm>();

        public VendorCreditClientStatusVm CreditClientStatus { get; set; } =
            new VendorCreditClientStatusVm();

        public IList<VendorCreditClientTrackingVm> CreditClientTracking { get; set; } =
            new List<VendorCreditClientTrackingVm>();
    }

    public class VendorCreditDashboardVm
    {
        public IList<VendorCreditClientCardVm> CreditClients { get; set; } =
            new List<VendorCreditClientCardVm>();

        public IList<VendorBillingStatementVm> BillingStatements { get; set; } =
            new List<VendorBillingStatementVm>();

        public IList<VendorPaymentReminderRuleVm> PaymentReminderRules { get; set; } =
            new List<VendorPaymentReminderRuleVm>();

        public VendorSettlementDiscountControlsVm SettlementDiscountControls { get; set; } =
            new VendorSettlementDiscountControlsVm();
    }

    public class VendorFeeDashboardVm
    {
        public decimal CashSaleFeeRate { get; set; }

        public decimal CreditSaleFeeRate { get; set; }

        public decimal TotalFeeAccumulated { get; set; }

        public string TotalFeeAccumulatedString { get; set; }

        public decimal FeeSettledThisWeek { get; set; }

        public string FeeSettledThisWeekString { get; set; }

        public IList<VendorOrderFeeVm> FeeChargedPerOrder { get; set; } =
            new List<VendorOrderFeeVm>();

        public IList<VendorWeeklyFeeStatementVm> WeeklyFeeStatements { get; set; } =
            new List<VendorWeeklyFeeStatementVm>();

        public string WeeklyFeeSettlementNote { get; set; }
    }

    public class VendorReportsDashboardVm
    {
        public VendorSalesReportVm SalesReport { get; set; } = new VendorSalesReportVm();

        public VendorCreditAgeingVm CreditAgeing { get; set; } = new VendorCreditAgeingVm();

        public VendorTaxReportVm TaxReport { get; set; } = new VendorTaxReportVm();

        public IList<VendorProductPerformanceVm> ProductPerformance { get; set; } =
            new List<VendorProductPerformanceVm>();

        public VendorDeliveryPerformanceVm DeliveryPerformance { get; set; } =
            new VendorDeliveryPerformanceVm();
    }

    public class VendorOffersDashboardVm
    {
        public IList<VendorOfferTypeVm> VendorSpecificOffers { get; set; } =
            new List<VendorOfferTypeVm>();

        public IList<VendorOfferDetailVm> ActiveVendorOffers { get; set; } =
            new List<VendorOfferDetailVm>();

        public IList<PlatformOfferDetailVm> PlatformOffers { get; set; } =
            new List<PlatformOfferDetailVm>();
    }

    public class MonthlyAmountVm
    {
        public string Month { get; set; }

        public int MonthNumber { get; set; }

        public decimal Amount { get; set; }

        public string AmountString { get; set; }
    }

    public class VendorInventoryProductVm
    {
        public long VendorProductId { get; set; }

        public long ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public string PriceString { get; set; }

        public int Stock { get; set; }

        public string Status { get; set; }

        public string BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

    public class VendorLowStockProductVm
    {
        public long VendorProductId { get; set; }

        public string ProductName { get; set; }

        public int Stock { get; set; }

        public string Status { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

    public class VendorRecentOrderVm
    {
        public long OrderId { get; set; }

        public string Buyer { get; set; }

        public string Status { get; set; }

        public decimal Amount { get; set; }

        public string AmountString { get; set; }

        public DateTime OrderedDate { get; set; }
    }

    public class VendorPendingOrderVm
    {
        public long OrderId { get; set; }

        public string Buyer { get; set; }

        public int Days { get; set; }

        public decimal Amount { get; set; }

        public string AmountString { get; set; }
    }

    public class VendorDispatchStatusVm
    {
        public int Processing { get; set; }

        public int ReadyToShip { get; set; }

        public int Delayed { get; set; }
    }

    public class VendorCreditBuyerSummaryVm
    {
        public int TotalBuyersOnCredit { get; set; }

        public IList<VendorCreditBuyerVm> Buyers { get; set; } = new List<VendorCreditBuyerVm>();
    }

    public class VendorCreditBuyerVm
    {
        public long CustomerId { get; set; }

        public string CustomerName { get; set; }

        public decimal Credit { get; set; }

        public string CreditString { get; set; }

        public string Status { get; set; }
    }

    public class VendorCreditSaleMatrixVm
    {
        public decimal TotalCreditSales { get; set; }

        public string TotalCreditSalesString { get; set; }

        public decimal PendingCredit { get; set; }

        public string PendingCreditString { get; set; }

        public decimal OverdueCredit { get; set; }

        public string OverdueCreditString { get; set; }
    }

    public class VendorCreditSettlementVm
    {
        public decimal TotalSettledCredit { get; set; }

        public string TotalSettledCreditString { get; set; }
    }

    public class VendorSalesReturnMatrixVm
    {
        public int TotalReturns { get; set; }

        public decimal ReturnValue { get; set; }

        public string ReturnValueString { get; set; }

        public int ReturnPending { get; set; }
    }

    public class VendorCreditClientStatusVm
    {
        public int WithinLimit { get; set; }

        public int NearLimit { get; set; }

        public int OverLimit { get; set; }
    }

    public class VendorCreditClientTrackingVm
    {
        public long CustomerId { get; set; }

        public string Client { get; set; }

        public decimal CreditLimit { get; set; }

        public string CreditLimitString { get; set; }

        public decimal Outstanding { get; set; }

        public string OutstandingString { get; set; }

        public decimal Overdue { get; set; }

        public string OverdueString { get; set; }

        public int Reminders { get; set; }
    }

    public class VendorCreditClientCardVm
    {
        public long CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string VendorCode { get; set; }

        public decimal CreditLimit { get; set; }

        public string CreditLimitString { get; set; }

        public DateTime From { get; set; }

        public DateTime? Till { get; set; }

        public string Status { get; set; }
    }

    public class VendorBillingStatementVm
    {
        public long CustomerId { get; set; }

        public string Buyer { get; set; }

        public long Invoice { get; set; }

        public DateTime InvoiceDate { get; set; }

        public DateTime DueDate { get; set; }

        public decimal Amount { get; set; }

        public string AmountString { get; set; }

        public string Status { get; set; }

        public decimal Settlement { get; set; }

        public string SettlementString { get; set; }

        public IList<string> Actions { get; set; } = new List<string>();
    }

    public class VendorPaymentReminderRuleVm
    {
        public string Name { get; set; }

        public string Timing { get; set; }

        public IList<string> Channels { get; set; } = new List<string>();

        public bool PenaltyApplicable { get; set; }
    }

    public class VendorSettlementDiscountControlsVm
    {
        public bool DiscountEnabled { get; set; }

        public decimal DiscountPercentage { get; set; }

        public int ValidityWindowDays { get; set; }

        public bool AutoDeductionEnabled { get; set; }
    }

    public class VendorOrderFeeVm
    {
        public long OrderId { get; set; }

        public string PaymentType { get; set; }

        public decimal OrderAmount { get; set; }

        public string OrderAmountString { get; set; }

        public decimal FeeRate { get; set; }

        public decimal FeeCharged { get; set; }

        public string FeeChargedString { get; set; }
    }

    public class VendorWeeklyFeeStatementVm
    {
        public string Week { get; set; }

        public int Orders { get; set; }

        public decimal TotalFee { get; set; }

        public string TotalFeeString { get; set; }
    }

    public class VendorSalesReportVm
    {
        public decimal TotalSales { get; set; }

        public string TotalSalesString { get; set; }

        public int TotalOrders { get; set; }

        public int RegionsCovered { get; set; }
    }

    public class VendorCreditAgeingVm
    {
        public decimal Days0To30 { get; set; }

        public decimal Days31To60 { get; set; }

        public decimal Days61To90 { get; set; }
    }

    public class VendorTaxReportVm
    {
        public decimal TaxableAmount { get; set; }

        public string TaxableAmountString { get; set; }

        public decimal TotalGst { get; set; }

        public string TotalGstString { get; set; }

        public decimal Cgst { get; set; }

        public string CgstString { get; set; }

        public decimal Sgst { get; set; }

        public string SgstString { get; set; }
    }

    public class VendorProductPerformanceVm
    {
        public string ProductName { get; set; }

        public decimal Revenue { get; set; }

        public string RevenueString { get; set; }

        public int QuantitySold { get; set; }
    }

    public class VendorDeliveryPerformanceVm
    {
        public int TotalDeliveries { get; set; }

        public int OnTime { get; set; }

        public int Delayed { get; set; }

        public decimal AverageDeliveryTimeDays { get; set; }
    }

    public class VendorOfferTypeVm
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int ActiveOfferCount { get; set; }
    }

    public class VendorOfferDetailVm
    {
        public long Id { get; set; }

        public long VendorId { get; set; }

        public string OfferType { get; set; }

        public string OfferTypeName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string TermsAndConditions { get; set; }

        public string DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public string MaxDiscountAmountString { get; set; }

        public decimal? MinimumOrderAmount { get; set; }

        public string MinimumOrderAmountString { get; set; }

        public int? MinimumQuantity { get; set; }

        public string CouponCode { get; set; }

        public DateTimeOffset? StartOn { get; set; }

        public DateTimeOffset? EndOn { get; set; }

        public bool IsActive { get; set; }

        public string Status { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }
    }

    public class VendorOfferFormVm
    {
        public string OfferType { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string TermsAndConditions { get; set; }

        public string DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public decimal? MinimumOrderAmount { get; set; }

        public int? MinimumQuantity { get; set; }

        public string CouponCode { get; set; }

        public DateTimeOffset? StartOn { get; set; }

        public DateTimeOffset? EndOn { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class PlatformOfferDetailVm
    {
        public string Code { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public IList<string> Benefits { get; set; } = new List<string>();

        public IList<string> Eligibility { get; set; } = new List<string>();

        public IList<string> Actions { get; set; } = new List<string>();

        public bool IsParticipating { get; set; }
    }
}
