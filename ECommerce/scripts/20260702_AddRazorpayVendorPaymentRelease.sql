IF OBJECT_ID(N'[dbo].[PaymentRazor_VendorPaymentRelease]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PaymentRazor_VendorPaymentRelease]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [OrderId] BIGINT NOT NULL,
        [VendorId] BIGINT NOT NULL,
        [FinancialPaymentId] BIGINT NULL,
        [BankDetailId] BIGINT NULL,
        [Amount] DECIMAL(18,4) NOT NULL,
        [Currency] NVARCHAR(10) NULL,
        [PaymentMethod] NVARCHAR(100) NULL,
        [Status] INT NOT NULL,
        [RazorpayContactId] NVARCHAR(450) NULL,
        [RazorpayFundAccountId] NVARCHAR(450) NULL,
        [RazorpayPayoutId] NVARCHAR(450) NULL,
        [RazorpayPayoutStatus] NVARCHAR(450) NULL,
        [ReferenceId] NVARCHAR(450) NULL,
        [FailureMessage] NVARCHAR(MAX) NULL,
        [RequestPayload] NVARCHAR(MAX) NULL,
        [ResponsePayload] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIMEOFFSET NOT NULL,
        [LatestUpdatedOn] DATETIMEOFFSET NOT NULL,
        [ReleasedOn] DATETIMEOFFSET NULL,
        CONSTRAINT [PK_PaymentRazor_VendorPaymentRelease] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentRazor_VendorPaymentRelease_Orders_Order_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders_Order] ([Id]),
        CONSTRAINT [FK_PaymentRazor_VendorPaymentRelease_Core_Vendor_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Core_Vendor] ([Id]),
        CONSTRAINT [FK_PaymentRazor_VendorPaymentRelease_FinancialPayment_FinancialPaymentId] FOREIGN KEY ([FinancialPaymentId]) REFERENCES [dbo].[FinancialPayment] ([Id]),
        CONSTRAINT [FK_PaymentRazor_VendorPaymentRelease_BankDetails_BankDetailId] FOREIGN KEY ([BankDetailId]) REFERENCES [dbo].[BankDetails] ([Id])
    );

    CREATE UNIQUE INDEX [IX_PaymentRazor_VendorPaymentRelease_OrderId]
        ON [dbo].[PaymentRazor_VendorPaymentRelease] ([OrderId]);

    CREATE INDEX [IX_PaymentRazor_VendorPaymentRelease_VendorId]
        ON [dbo].[PaymentRazor_VendorPaymentRelease] ([VendorId]);

    CREATE INDEX [IX_PaymentRazor_VendorPaymentRelease_FinancialPaymentId]
        ON [dbo].[PaymentRazor_VendorPaymentRelease] ([FinancialPaymentId]);

    CREATE INDEX [IX_PaymentRazor_VendorPaymentRelease_BankDetailId]
        ON [dbo].[PaymentRazor_VendorPaymentRelease] ([BankDetailId]);

    CREATE INDEX [IX_PaymentRazor_VendorPaymentRelease_RazorpayPayoutId]
        ON [dbo].[PaymentRazor_VendorPaymentRelease] ([RazorpayPayoutId]);
END;

UPDATE [dbo].[Payments_PaymentProvider]
SET [AdditionalSettings] = JSON_MODIFY(
    JSON_MODIFY(
        ISNULL(NULLIF([AdditionalSettings], ''), N'{}'),
        '$.PayoutAccountNumber',
        ISNULL(JSON_VALUE([AdditionalSettings], '$.PayoutAccountNumber'), '')
    ),
    '$.PayoutMode',
    ISNULL(JSON_VALUE([AdditionalSettings], '$.PayoutMode'), 'IMPS')
)
WHERE [Id] = N'Razorpay';
