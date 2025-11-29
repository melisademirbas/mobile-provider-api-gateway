using Microsoft.EntityFrameworkCore;
using MobileProviderApi.Models;

namespace MobileProviderApi.Data
{
    public class MobileProviderContext : DbContext
    {
        public MobileProviderContext(DbContextOptions<MobileProviderContext> options)
            : base(options)
        {
        }

        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<Bill> Bills { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SubscriberNo'yu birincil anahtar olarak belirliyoruz.
            modelBuilder.Entity<Subscriber>()
                .HasKey(s => s.SubscriberNo);

            // Bill modelinde decimal alanların hassasiyetini belirliyoruz (Örn: 18 basamak, virgülden sonra 2 basamak)
            modelBuilder.Entity<Bill>()
                .Property(b => b.BillTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.RemainingAmount)
                .HasPrecision(18, 2);

            // Bill ile Subscriber arasındaki ilişkiyi netleştirme (Gölge Özellik hatasını giderir)
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Subscriber) // Bill bir Subscriber'a sahiptir
                .WithMany(s => s.Bills)    // Subscriber birden çok Bill'e sahiptir
                .HasForeignKey(b => b.SubscriberNo) // Foreign Key olarak Bill.SubscriberNo'yu kullan
                .IsRequired(); // İlişkinin zorunlu olduğunu belirtiyoruz
        }
    }
}