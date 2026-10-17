using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Message 
    {
        public int Id { get; set; }

        // Foreign key -> ApplicationUser
        [Required]
        public required string SenderId { get; set; }
        public ApplicationUser Sender { get; set; } = null!;

        [Required]
        public required string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public required string Content { get; set; }

        public DateTime Timestamp { get; set; }
    }
}