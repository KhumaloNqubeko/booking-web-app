using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Venue> Venues { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<EventType> EventTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventType>()
                .Property(e => e.Name)
                .HasMaxLength(80);

            modelBuilder.Entity<EventType>()
                .HasData(EventTypeCatalog.All.Select(item => new EventType
                {
                    Id = item.Id,
                    Name = item.Name
                }));

            modelBuilder.Entity<Event>()
                .HasOne<EventType>()
                .WithMany()
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne<Event>()
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne<Venue>()
                .WithMany()
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Venue>()
                .Property(v => v.Availability)
                .HasMaxLength(32);

            if (Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
            {
                modelBuilder.Entity<Booking>()
                    .Property(b => b.BookingDate)
                    .HasColumnType("date");

                modelBuilder.Entity<Event>()
                    .Property(e => e.StartDateTime)
                    .HasColumnType("datetime2");

                modelBuilder.Entity<Event>()
                    .Property(e => e.EndDateTime)
                    .HasColumnType("datetime2");
            }
            else
            {
                modelBuilder.Entity<Booking>()
                    .Property(b => b.BookingDate)
                    .HasColumnType("date");

                modelBuilder.Entity<Event>()
                    .Property(e => e.StartDateTime)
                    .HasColumnType("timestamp without time zone");

                modelBuilder.Entity<Event>()
                    .Property(e => e.EndDateTime)
                    .HasColumnType("timestamp without time zone");
            }
        }
    }
}
