using Staybnb.Enums;

namespace Staybnb.Models
{
    public class HostApplication
    {
        public int Id { get; set; }

        // Foreign key -> ApplicationUser
        public required string ApplicationUserId { get; set; }
        public ApplicationUser Applicant { get; set; } = null!;

        // Foreign key -> HostProperty
        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime AppliedAt { get; set; }
    }
}