namespace MobileProviderApi.Models
{
    public class Subscriber
    {
        public int SubscriberNo { get; set; } // Primary Key (Birincil Anahtar)
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        // EF Core için ilişki: Bir abonenin birden çok faturası vardır.
        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}