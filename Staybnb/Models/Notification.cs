using System.ComponentModel.DataAnnotations;
using Staybnb.Enums;

namespace Staybnb.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // Foreign key -> ApplicationUser
        public required string UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        [StringLength(100)]
        public required string Title { get; set; }

        public NotificationType Type { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}