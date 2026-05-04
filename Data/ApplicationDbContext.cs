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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
