using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Review
    {
        public int Id { get; set; }

        // Foreign key -> HostProperty
        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        // Foreign key -> Guest's user ID
        [Required]
        public required string ReviewerId { get; set; }
        public ApplicationUser Reviewer { get; set; } = null!;

        // Rating must range from 1 to 5
        [Range(1, 5, ErrorMessage = "Rating must range from 1 to 5.")]
        public int Rating { get; set; }      

        [Required]
        [StringLength(1000)]
        public required string Comment { get; set; } 

        public DateTime CreatedAt { get; set; }
    }
}