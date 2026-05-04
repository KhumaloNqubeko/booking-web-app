using Booking_webapp.Data;
using Booking_webapp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var recentBookings = await (
                from booking in _context.Bookings.AsNoTracking()
                join venue in _context.Venues.AsNoTracking() on booking.VenueId equals venue.Id into venueJoin
                from venue in venueJoin.DefaultIfEmpty()
                join evnt in _context.Events.AsNoTracking() on booking.EventId equals evnt.Id into eventJoin
                from evnt in eventJoin.DefaultIfEmpty()
                orderby booking.BookingDate descending
                select new DashboardBookingViewModel
                {
                    Id = booking.Id,
                    VenueName = venue != null ? venue.Name : "Unknown venue",
                    EventName = evnt != null ? evnt.Name : "Unknown event",
                    BookingDate = booking.BookingDate,
                    Status = booking.Status
                })
                .Take(5)
                .ToListAsync();

            var model = new HomeDashboardViewModel
            {
                VenueCount = await _context.Venues.CountAsync(),
                EventCount = await _context.Events.CountAsync(),
                BookingCount = await _context.Bookings.CountAsync(),
                RecentBookings = recentBookings
            };

            return View(model);
        }
    }
}
