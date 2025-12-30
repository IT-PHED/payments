using System.ComponentModel.DataAnnotations;

namespace PhedPay.Models
{

    


        public class CustomerLookupRequest
        {
            
            public string Username { get; set; } 
            public string apikey { get; set; } 
            public string CustomerNumber { get; set; }
            public string Mobile_Number { get; set; }
            public string Mailid { get; set; }
            public string CustomerType { get; set; } = "";
        }



   

}

