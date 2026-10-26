using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Staybnb.Models
{
    public class HostApplicationViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        [Display(Name = "Price per night")]
        public decimal PricePerNight { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Property type")]
        public string PropertyType { get; set; } = string.Empty;

        [Range(0, 1000000)]
        [Display(Name = "Cleaning fee")]
        public decimal CleaningFee { get; set; }

        [Range(0, 1000000)]
        [Display(Name = "Service fee")]
        public decimal ServiceFee { get; set; }

        [Required(ErrorMessage = "Upload at least one propery image.")]
        [MinLength(1, ErrorMessage = "Upload at least one property image.")]
        [Display(Name = "Property images")]
        public List<IFormFile> Images { get; set; } = new();
    }
}