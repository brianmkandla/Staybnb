using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Amenity
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; }

        [Required]
        [StringLength(50)]
        public required string IconClass { get; set; }

        public ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
    }
}