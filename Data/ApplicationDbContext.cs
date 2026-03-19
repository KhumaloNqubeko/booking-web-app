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
    }
}
