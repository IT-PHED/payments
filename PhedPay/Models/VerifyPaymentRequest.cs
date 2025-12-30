using Newtonsoft.Json;

namespace PhedPay.Models
{
    public class VerifyPaymentRequest
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }
    }
}


