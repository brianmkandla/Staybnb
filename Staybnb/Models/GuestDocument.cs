using System.ComponentModel.DataAnnotations;
using Staybnb.Enums;

namespace Staybnb.Models
{
    public class GuestDocument
    {
        public int Id { get; set; }

        // Foreign key -> GuestCheckIn
        public int GuestCheckInId { get; set; }
        public GuestCheckIn GuestCheckIn { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public required string DocumentType { get; set; }

        [Required]
        [StringLength(200)]
        public required string FileName { get; set; }

        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    }
}