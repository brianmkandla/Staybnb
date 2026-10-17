using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class CheckInProcess
    {
        public int Id { get; set; }

        // Foreign key -> HostProperty
        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        [StringLength(200)]
        public required string Title { get; set; }
        public required string StepsJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}