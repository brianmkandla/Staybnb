using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }

        // Foreign Key -> HostProperty
        public int PropertyId { get; set; }

        // Navigation property
        public HostProperty? Property { get; set; }

        [Required]
        public required string ImageUrl { get; set; }
    }
}