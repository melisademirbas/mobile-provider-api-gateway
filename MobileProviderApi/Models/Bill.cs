using System.ComponentModel.DataAnnotations;

namespace MobileProviderApi.Models
{
    public class Bill
    {
        public int BillId { get; set; } // Primary Key
        public int SubscriberNo { get; set; } // Foreign Key 
        
        public string Month { get; set; } = string.Empty; 
        public decimal BillTotal { get; set; } // DECIMAL uyarısını gidermek için HasPrecision kullanıldı
        public string? BillDetails { get; set; } 
        public string PaidStatus { get; set; } = "Unpaid"; 
        public decimal RemainingAmount { get; set; } = 0; // DECIMAL uyarısını gidermek için HasPrecision kullanıldı

        // İlişki (Navigation Property)
        public Subscriber? Subscriber { get; set; } 
    }
}