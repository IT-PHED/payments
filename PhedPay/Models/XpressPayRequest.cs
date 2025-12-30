namespace PhedPay.Models
{
  
    public class XpressPayRequest
    {
        public string amount { get; set; }
        public string email { get; set; }
        public string transactionId { get; set; }
        public string currency { get; set; }=   "NGN";
        public string productId { get; set; } = "1001";
        public string productDescription { get; set; } = "Payment for PHED Energy";

    }

}
