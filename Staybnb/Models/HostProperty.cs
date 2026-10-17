using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staybnb.Models
{
    public class HostProperty
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        public required string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        [Required]
        public required string Address { get; set; }

        [Required]
        [StringLength(100)]
        public required string City { get; set; }

        [Required]
        [StringLength(50)]
        public required string PropertyType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CleaningFee { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceFee { get; set; } = 0m;
        
        // Foreign key -> Host
        public required string HostId { get; set; }
        public ApplicationUser Host { get; set; } = null!;
  
        public bool IsActive { get; set; } = true;

        public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    }
}