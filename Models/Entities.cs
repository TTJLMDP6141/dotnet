using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace UrbanServicesMvc.Models;

public sealed class AppUser : IdentityUser
{
    [Required, StringLength(80)]
    public string FullName { get; set; } = "";
    public ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();
}

public sealed class ServiceOffering
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    [Required, StringLength(1000)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string Category { get; set; } = "";
    [Range(1, 1000000)]
    public decimal Price { get; set; }
    [Range(1, 1440)]
    public int DurationMinutes { get; set; }
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    public bool Active { get; set; } = true;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public sealed class ProviderProfile
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    [Range(0, 80)]
    public int Experience { get; set; }
    [Required, StringLength(200)]
    public string Availability { get; set; } = "Monday to Saturday";
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public enum BookingStatus
{
    PENDING,
    ACCEPTED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

public sealed class Booking
{
    public int Id { get; set; }
    [Required]
    public string CustomerId { get; set; } = "";
    public AppUser Customer { get; set; } = null!;
    public int ServiceId { get; set; }
    public ServiceOffering Service { get; set; } = null!;
    public int? ProviderProfileId { get; set; }
    public ProviderProfile? ProviderProfile { get; set; }
    public DateTime BookingDate { get; set; }
    [Required, StringLength(20)]
    public string BookingTime { get; set; } = "";
    [Required, StringLength(300)]
    public string Address { get; set; } = "";
    [Required, StringLength(24)]
    public string Phone { get; set; } = "";
    public BookingStatus Status { get; set; } = BookingStatus.PENDING;
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Review? Review { get; set; }
}

public sealed class Review
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    [Required]
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public int ServiceId { get; set; }
    public ServiceOffering Service { get; set; } = null!;
    [Range(1, 5)]
    public int Rating { get; set; }
    [Required, StringLength(1000)]
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
