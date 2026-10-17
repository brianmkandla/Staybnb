namespace Staybnb.Models
{
    public class WishlistItem 
    {
        public int Id { get; set; }

        // Foreign key -> ApplicationUser
        public required string UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        // Foreign key -> HostProperty
        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}