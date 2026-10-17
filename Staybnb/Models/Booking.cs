using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Enums;

namespace Staybnb.Models
{
    public class Booking
    {
        public int Id { get; set; }

        // Foreign key -> HostProperty
        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        // Foreign key -> ApplicationUser
        public required string GuestId { get; set; }
        public ApplicationUser Guest  { get; set;} = null!;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public int NumberOfTravelers { get; set; } = 1;

        public BookingStatus Status { get; set; } = BookingStatus.Pending;
    }
}