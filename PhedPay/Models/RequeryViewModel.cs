namespace PhedPay.Models
{
    
    public class RequeryViewModel
    {
        public string SearchKey { get; set; } // To show what was searched
        public List<TransactionEntity> LocalTransactions { get; set; }
        public List<Payment> OraclePayments { get; set; } // Assuming 'Payment' is your Oracle model class
    }
}
