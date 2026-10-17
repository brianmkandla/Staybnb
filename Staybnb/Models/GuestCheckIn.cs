using Staybnb.Enums;

namespace Staybnb.Models
{
    public class GuestCheckIn
    {
        public int Id { get; set; }

        // Foreign key -> Booking
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        // Foreign key -> CheckInProcess
        public int CheckInProcessId { get; set; }
        public CheckInProcess CheckInProcess { get; set; } = null!;
        public CheckInStatus Status { get; set; } = CheckInStatus.NotStarted;
        public DateTime CreatedAt { get; set; }
    }
}