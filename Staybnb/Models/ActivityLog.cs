using System.ComponentModel.DataAnnotations;
using Staybnb.Enums;

namespace Staybnb.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        // Foreign key -> ApplicationUser
        public required string UserId { get; set; }
        public ApplicationUser User { get; set; }= null!;
        
        [StringLength(100)]
        public required string Action { get; set; }
        public ActivityType ActivityType { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}