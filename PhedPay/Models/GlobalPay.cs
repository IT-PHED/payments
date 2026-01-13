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
        public string lastName { get; set; }
        public string firstName { get; set; }
        public string currency { get; set; } = "NGN"; // Default to Naira
        public string phoneNumber { get; set; }
        public string address { get; set; }
        public string emailAddress { get; set; }
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
        public bool isSuccessful { get; set; }
        public string responseCode { get; set; } // "00" or "0000" usually means success
        public string successMessage { get; set; }
        public GlobalPayQueryData data { get; set; }
    }

    public class GlobalPayQueryData
    {
        public string transactionReference { get; set; }
        public string paymentStatus { get; set; } // "Successful", "Failed", etc.
        public decimal amount { get; set; }
        public string currency { get; set; }
    }
}


