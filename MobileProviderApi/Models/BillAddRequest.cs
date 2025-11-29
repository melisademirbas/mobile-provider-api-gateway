namespace MobileProviderApi.Models
{
    // Admin - Add Bill API'sinin vücudunda (body) beklediği veri yapısı
    public class BillAddRequest
    {
        public int SubscriberNo { get; set; }
        public string Month { get; set; } = string.Empty;
        public decimal BillTotal { get; set; }
        public string? BillDetails { get; set; }
    }
}