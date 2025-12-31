using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhedPay.Models
{
    [Table("PaymentTransactions")] // Maps to the SQL table name
    public class TransactionEntity
    {
        [Key] // This is now the Auto-Increment ID
        public int Id { get; set; }
        public string TransactionReference { get; set; }
        public Guid GlobalId { get; set; }

        public string MeterNo { get; set; }
        public string AccountNo { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal Amount { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public bool IsSynced { get; set; }
        public DateTime? SyncedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}




