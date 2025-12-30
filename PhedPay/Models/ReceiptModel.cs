namespace PhedPay.Models
{
    public class ReceiptModel
    {
    }
    public class PhedReceiptResponse
    {
        // The API returns an array, so we will deserialize to List<PhedReceiptItem>
    }

    public class PhedReceiptItem
    {
        public string CUSTOMER_NO { get; set; }
        public string METER_NO { get; set; }
        public string RECEIPTNUMBER { get; set; }
        public string PAYMENTDATETIME { get; set; }
        public string AMOUNT { get; set; }
        public string TOKENDESC { get; set; } // <--- THE TOKEN
        public string UNITSACTUAL { get; set; }
        public string TARIFF { get; set; }
        public string STATUS { get; set; }
        public string CONS_NAME { get; set; }
        public string ADDRESS { get; set; }

        // The nested array for cost breakdown
        public List<PhedReceiptDetail> DETAILS { get; set; }
    }

    public class PhedReceiptDetail
    {
        public string HEAD { get; set; }   // e.g. "VAT (NGN)"
        public string AMOUNT { get; set; } // e.g. "3.49"
    }
}
