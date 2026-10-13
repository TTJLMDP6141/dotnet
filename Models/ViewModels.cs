using System.ComponentModel.DataAnnotations;

namespace UrbanServicesMvc.Models;

public sealed class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required]
    public string AccountType { get; set; } = "CUSTOMER";
    public bool RememberMe { get; set; }
}

public sealed class RegisterVm
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = "";
    [Required, StringLength(24, MinimumLength = 7)]
    public string Phone { get; set; } = "";
    [Required]
    public string Role { get; set; } = "CUSTOMER";
    [DataType(DataType.Password), Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
    [Range(0, 80)]
    public int Experience { get; set; }
    [StringLength(200)]
    public string Availability { get; set; } = "Monday to Saturday";
    [DataType(DataType.Password), StringLength(128)]
    public string? AdminInviteCode { get; set; }
}

public sealed class BookingVm
{
    public int ServiceId { get; set; }
    [DataType(DataType.Date), Required]
    public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);
    [Required]
    public string BookingTime { get; set; } = "";
    [Required, StringLength(300, MinimumLength = 5)]
    public string Address { get; set; } = "";
    [Required, StringLength(24, MinimumLength = 7)]
    public string Phone { get; set; } = "";
}

public sealed class ReviewVm
{
    public int BookingId { get; set; }
    [Range(1, 5)]
    public int Rating { get; set; } = 5;
    [Required, StringLength(1000, MinimumLength = 3)]
    public string Comment { get; set; } = "";
}

public sealed class ProfileVm
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = "";
    [Required, StringLength(24, MinimumLength = 7)]
    public string Phone { get; set; } = "";
}

public sealed class ProviderProfileVm
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string FullName { get; set; } = "";
    [Required, StringLength(24, MinimumLength = 7)]
    public string Phone { get; set; } = "";
    [Range(0, 80)]
    public int Experience { get; set; }
    [Required, StringLength(200)]
    public string Availability { get; set; } = "";
}

public sealed class ServiceVm
{
    public int Id { get; set; }
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string Category { get; set; } = "";
    [Range(1, 1000000)]
    public decimal Price { get; set; }
    [Range(1, 1440)]
    public int DurationMinutes { get; set; } = 60;
    [Url, StringLength(500)]
    public string? ImageUrl { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class ProviderDashboardVm
{
    public ProviderProfile Provider { get; set; } = null!;
    public List<Booking> Bookings { get; set; } = [];
}

public sealed class AdminDashboardVm
{
    public int CustomerCount { get; set; }
    public int ProviderCount { get; set; }
    public int ServiceCount { get; set; }
    public int BookingCount { get; set; }
    public List<Booking> RecentBookings { get; set; } = [];
}

public sealed class AdminBookingsVm
{
    public List<Booking> Bookings { get; set; } = [];
    public List<ProviderProfile> Providers { get; set; } = [];
}

public sealed class ServiceDetailsVm
{
    public ServiceOffering Service { get; set; } = null!;
    public List<Review> Reviews { get; set; } = [];
}
