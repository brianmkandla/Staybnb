using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;

namespace Staybnb.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<HostProperty> HostProperties => Set<HostProperty>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<HostApplication> HostApplications => Set<HostApplication>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
        public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
        public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();
    
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // A host can own many proterties
            builder.Entity<HostProperty>()
                .HasOne(p => p.Host)
                .WithMany()
                .HasForeignKey(p => p.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            // A property can have multiple images
            builder.Entity<PropertyImage>()
                .HasOne(i => i.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            // A host application belongs to its applicant and property
            builder.Entity<HostApplication>()
                .HasOne(a => a.Applicant)
                .WithMany()
                .HasForeignKey(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<HostApplication>()
                .HasOne(a => a.Property)
                .WithMany()
                .HasForeignKey(a => a.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            // A booking belongs to a property and a guest.
            builder.Entity<Booking>()
                .HasOne(b => b.Property)
                .WithMany()
                .HasForeignKey(b => b.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.Guest)
                .WithMany()
                .HasForeignKey(b => b.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            // A booking can have one payment
            builder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // A property can receive multiple reviews.
            builder.Entity<Review>()
                .HasOne(r => r.Property)
                .WithMany()
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Properties and amenities have a many-to-many relationship.
            builder.Entity<HostProperty>()
                .HasMany<Amenity>()
                .WithMany(a => a.Properties);

            // A wishlist item belongs to a user and property.
            builder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne(w => w.Property)
                .WithMany()
                .HasForeignKey(w => w.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Explicitly distinguish message sender and receiver.
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Each property has one check-in process.
            builder.Entity<CheckInProcess>()
                .HasOne(c => c.Property)
                .WithOne()
                .HasForeignKey<CheckInProcess>(c => c.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Each booking has one guest check-in.
            builder.Entity<GuestCheckIn>()
                .HasOne(g => g.Booking)
                .WithOne()
                .HasForeignKey<GuestCheckIn>(g => g.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Multiple guest check-ins can use the same property's process.
            builder.Entity<GuestCheckIn>()
                .HasOne(g => g.CheckInProcess)
                .WithMany()
                .HasForeignKey(g => g.CheckInProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            // A guest check-in can contain multiple documents.
            builder.Entity<GuestDocument>()
                .HasOne(d => d.GuestCheckIn)
                .WithMany()
                .HasForeignKey(d => d.GuestCheckInId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
