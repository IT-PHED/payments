namespace PhedPay.Models
{
    public class PhedNotificationRequest
    {
        
        public string Username { get; set; } 
        public string apikey { get; set; }
        public string PaymentLogId { get; set; }
        public string CustReference { get; set; }
        public string AlternateCustReference { get; set; }
        public string Amount { get; set; }
        public string PaymentMethod { get; set; } = "WEB";
        public string PaymentReference { get; set; }
        public string TerminalID { get; set; }
        public string ChannelName { get; set; } = "WEB";
        public string Location { get; set; }
        public string PaymentDate { get; set; }
        public string BankName { get; set; } = "PHED WEB";
        public string BranchName { get; set; } = "PHED WEB";
        public string CustomerName { get; set; }
        public string ReceiptNo { get; set; }
        public string BankCode { get; set; } = "023";
        public string CustomerAddress { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public string DepositorName { get; set; }
        public string DepositSlipNumber { get; set; }
        public string PaymentCurrency { get; set; } = "NGN";
        public string ItemName { get; set; } = "PHED Bill Payment";
        public string ItemCode { get; set; } = "01";
        public string ItemAmount { get; set; }
        public string PaymentStatus { get; set; } = "Success";
        public string SettlementDate { get; set; }
        public string Teller { get; set; } = "PHED WebTeller";
        public string OtherCustomerInfo { get; set; }
        public string IsReversal { get; set; }
        public string InstitutionName { get; set; }
        public string CollectionsAccount { get; set; }
        public string InstitutionId { get; set; }
    }
}

