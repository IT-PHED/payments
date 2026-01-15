using Newtonsoft.Json;
using System.Text.Json.Serialization;


namespace PhedPay.Models
{
    public class GlobalPay
    {
    }
    public class GlobalPayRequest
    {
        public decimal amount { get; set; }
        public string merchantTransactionReference { get; set; }
        public string redirectUrl { get; set; }
        public GlobalPayCustomer customer { get; set; }
    }

public class GlobalPayCustomer
    {
        // C# uses FirstName (Pascal), JSON uses firstName (camel)
        [JsonProperty("firstName")]
        [JsonPropertyName("firstName")]
        public string firstName { get; set; }

        [JsonProperty("lastName")]
        [JsonPropertyName("lastName")]
        public string lastName { get; set; }

        [JsonProperty("currency")]
        [JsonPropertyName("currency")]
        public string currency { get; set; } = "NGN";

        [JsonProperty("phoneNumber")]
        [JsonPropertyName("phoneNumber")]
        public string phoneNumber { get; set; }

        [JsonProperty("address")]
        [JsonPropertyName("address")]
        public string address { get; set; }

        [JsonProperty("emailAddress")]
        [JsonPropertyName("emailAddress")]
        public string emailAddress { get; set; }

        [JsonProperty("paymentFormCustomFields")]
        [JsonPropertyName("paymentFormCustomFields")]
        public List<GlobalPayCustomField> paymentFormCustomFields { get; set; }
    }
    public class GlobalPayCustomField
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    public class GlobalPayInitResponse
    {
        public bool isSuccessful { get; set; }
        public GlobalPayInitData data { get; set; }
        public string successMessage { get; set; }
        public string responseCode { get; set; }
    }

    public class GlobalPayInitData
    {
        public string checkoutUrl { get; set; } // <--- This is where we need to go
        public string accessCode { get; set; }
        public string transactionReference { get; set; }
    }

   
    public class GlobalPayQueryResponse
    {
        // FIX 1: Change 'GlobalPayQueryData' to 'List<GlobalPayQueryData>'
        public List<GlobalPayQueryData> data { get; set; }

        public string successMessage { get; set; }
        public string responseCode { get; set; }
        public bool isSuccessful { get; set; }
    }

    public class GlobalPayQueryData
    {
        // FIX 2: Match the exact JSON property names
        public string merchantTxnref { get; set; } // Matches "merchantTxnref"
        public string transactionStatus { get; set; } // Matches "transactionStatus"
        public decimal amountFromMerchant { get; set; }
        public string transactionDate { get; set; }
        public string merchantAccountNumber { get; set; }
    }
}


