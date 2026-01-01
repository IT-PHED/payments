namespace PhedPay.Models
{
    public class Payment
    {
        // Core Identifiers
        public string ConsumerNo { get; set; } = null!;
        public string ReceiptNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PaymentDateTime { get; set; }
        public string PaymentModes { get; set; } = null!;
        public string PaymentPurpose { get; set; } = null!;
        public string PayThrough { get; set; } = null!;
        public int Status { get; set; }
        public long Pid { get; set; }

        // Customer / Reference Info
        public string? CustName { get; set; }
        public string? LastInvoiceId { get; set; }
        public string? ApplicationNo { get; set; }
        public string? ManualBookNo { get; set; }
        public string? ManualReceiptNo { get; set; }

        // Bank / Batch
        public string? BankId { get; set; }
        public string? BatchLabel { get; set; }
        public DateTime? BatchLabelDate { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? ChequeNo { get; set; }
        public string? DcSheetNo { get; set; }
        public int? OnlineOffline { get; set; }

        // Posting / Audit
        public DateTime? PaymentUploadDateTime { get; set; }
        public string? Posted { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? CreatedBy { get; set; }

        // Amount Breakdown
        public decimal? CapitalAmount { get; set; }
        public decimal? MiscAmount { get; set; }
        public decimal? RefundAmount { get; set; }
        public decimal? VendingAmount { get; set; }
        public decimal? FactorAmount { get; set; }
        public decimal? StampDuty { get; set; }
        public decimal? ActualAmtPaid { get; set; }

        // Collector / Agent
        public string? SbmNo { get; set; }
        public string? CollectorId { get; set; }
        public string? CollectorName { get; set; }
        public int? AgentFlag { get; set; }

        // GPS / Device
        public string? Longitude { get; set; }
        public string? Latitude { get; set; }
        public DateTime? GpsTime { get; set; }
        public string? BatteryStatus { get; set; }
        public string? SignalStrength { get; set; }
        public string? PrinterBattery { get; set; }
        public string? CurVersion { get; set; }
        public string? Altitude { get; set; }
        public string? UserAccuracy { get; set; }

        // Consumption / Billing
        public DateTime? BillMonthFor { get; set; }
        public int? TotalConsumption { get; set; }
        public int? AppConsumption { get; set; }
        public int? App1Consumption { get; set; }
        public string? App1Name { get; set; }
        public int? App2Consumption { get; set; }
        public string? App2Name { get; set; }
        public int? App3Consumption { get; set; }
        public string? App3Name { get; set; }

        // Temporary Connections
        public string? LstSession { get; set; }
        public DateTime? TmpConStartDate { get; set; }
        public DateTime? TmpConEndDate { get; set; }
        public string? TmpConRefNo { get; set; }

        // Demand / Tariff
        public string? DemandNo { get; set; }
        public string? DcCode { get; set; }
        public string? Tariff { get; set; }
        public string? SubClass { get; set; }
        public string? Description { get; set; }
        public string? TariffIndex { get; set; }

        // Identity / Contact
        public string? AdhrNo { get; set; }
        public string? EmailId { get; set; }
        public string? MobileNo { get; set; }

        // Token / Meter
        public string? TokenDec { get; set; }
        public string? TokenHex { get; set; }
        public string? MeterNo { get; set; }
        public string? VendTimeUnix { get; set; }
        public string? UnitsActual { get; set; }
        public string? UnitName { get; set; }
        public string? ValueActual { get; set; }

        // Card / POS
        public string? TranId { get; set; }
        public string? ChannelName { get; set; }
        public decimal? CardTranAmount { get; set; }
        public string? TransactionStatus { get; set; }
        public string? Rrn { get; set; }
        public string? CardNumber { get; set; }
        public string? TerminalId { get; set; }
        public string? IsPos { get; set; }

        // Refund / Notification
        public int? NormalRefund { get; set; }
        public int? CapmiRefund { get; set; }
        public string? Email { get; set; }
        public string? Sms { get; set; }

        // Consumer Classification
        public string? ConsType { get; set; }
        public string? ConsCategory { get; set; }
        public string? StampDutyCode { get; set; }

        // Relationships / Commission
        public string? UniquePayment { get; set; }
        public string? ParentId { get; set; }
        public int? FeederLocationClassId { get; set; }
        public decimal? CommissionPercentage { get; set; }
        public decimal? Commission { get; set; }
        public decimal? Commission2 { get; set; }

        // Misc
        public string? IdRecord { get; set; }
        public int? FactorType { get; set; }
    }

}
