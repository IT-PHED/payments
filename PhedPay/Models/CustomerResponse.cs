public class CustomerResponse
{
    public string status { get; set; }
    public string ibc_name { get; set; }
    public string bsc_name { get; set; }
    public string customer_name { get; set; }
    public string meter_no { get; set; }
    public string customer_no { get; set; }
    public string mobile_no { get; set; }
    public string customer_type { get; set; }
    public decimal arrear { get; set; }
    public decimal current_amount { get; set; }
    public decimal total_bill { get; set; }
    public string address { get; set; }
    public decimal factor_amount { get; set; }
    public int md_flag { get; set; }
    public string tariff_code { get; set; }
    public string offgrid { get; set; }
    public List<Payable> payables { get; set; }
}

public class Payable
{
    public int purposeId { get; set; }
    public string purposeName { get; set; }
    public decimal totalAmount { get; set; }
}