namespace PhedPay.Models
{
    public class XpressPayResponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public XpressPayData data { get; set; }
    }

    public class XpressPayData
    {
        public string paymentUrl { get; set; }
        public string accessCode { get; set; }
    }
}


