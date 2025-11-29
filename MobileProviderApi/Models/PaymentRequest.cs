namespace MobileProviderApi.Models
{
    // PayBill API'sinin vücudunda (body) beklediği veri yapısı
    public class PaymentRequest
    {
        public int SubscriberNo { get; set; }
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; } // Ödenen miktar
    }
}