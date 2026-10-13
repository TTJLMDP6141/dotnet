using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrbanServicesMvc.Models;

namespace UrbanServicesMvc.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<ServiceOffering> Services => Set<ServiceOffering>();
    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ServiceOffering>().Property(x => x.Price).HasPrecision(10, 2);
        builder.Entity<Booking>().Property(x => x.TotalPrice).HasPrecision(10, 2);
        builder.Entity<ProviderProfile>().HasIndex(x => x.UserId).IsUnique();
        builder.Entity<Review>().HasIndex(x => x.BookingId).IsUnique();
        builder.Entity<Booking>()
            .HasOne(x => x.Service)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Booking>()
            .HasOne(x => x.ProviderProfile)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.ProviderProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
