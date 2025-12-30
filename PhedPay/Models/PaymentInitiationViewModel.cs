using System.ComponentModel.DataAnnotations;

namespace PhedPay.Models
{

    public class PaymentInitiationViewModel
    {
        [Required] public string MeterNo { get; set; }
        [Required] public string PhoneNumber { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public decimal Amount { get; set; }
    }
}
