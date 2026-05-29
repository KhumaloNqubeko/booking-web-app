using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        public readonly ApplicationDbContext dbContext;

        public BookingController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetBooking(Guid id)
        {
            var booking = await dbContext.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound(new { message = "The requested booking was not found." });
            }

            return Ok(booking);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBookings(
            Guid? venueId = null,
            int? eventTypeId = null,
            string? venueAvailability = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? status = null)
        {
            var bookingQuery =
                from booking in dbContext.Bookings.AsNoTracking()
                join venue in dbContext.Venues.AsNoTracking() on booking.VenueId equals venue.Id
                join evnt in dbContext.Events.AsNoTracking() on booking.EventId equals evnt.Id
                select new
                {
                    Booking = booking,
                    EventTypeId = evnt.EventTypeId,
                    VenueAvailability = venue.Availability
                };

            if (venueId.HasValue)
            {
                bookingQuery = bookingQuery.Where(item => item.Booking.VenueId == venueId.Value);
            }

            if (eventTypeId.HasValue)
            {
                bookingQuery = bookingQuery.Where(item => item.EventTypeId == eventTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(venueAvailability))
            {
                bookingQuery = bookingQuery.Where(item => item.VenueAvailability == venueAvailability);
            }

            if (dateFrom.HasValue)
            {
                bookingQuery = bookingQuery.Where(item => item.Booking.BookingDate.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                bookingQuery = bookingQuery.Where(item => item.Booking.BookingDate.Date <= dateTo.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                bookingQuery = bookingQuery.Where(item => item.Booking.Status == status);
            }

            var bookings = await bookingQuery
                .Select(item => item.Booking)
                .ToListAsync();

            return Ok(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> PostBooking(BookingDto bookingDto)
        {
            await ValidateBookingAsync(bookingDto);

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var booking = new Booking
            {
                VenueId = bookingDto.VenueId,
                EventId = bookingDto.EventId,
                BookingDate = bookingDto.BookingDate,
                Status = bookingDto.Status
            };

            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateBooking(Guid id, BookingDto bookingDto)
        {
            var booking = await dbContext.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound(new { message = "The requested booking was not found." });
            }

            await ValidateBookingAsync(bookingDto, id);

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            booking.VenueId = bookingDto.VenueId;
            booking.EventId = bookingDto.EventId;
            booking.BookingDate = bookingDto.BookingDate;
            booking.Status = bookingDto.Status;

            await dbContext.SaveChangesAsync();

            return Ok(booking);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteBooking(Guid id)
        {
            var booking = await dbContext.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound(new { message = "The requested booking was not found." });
            }

            dbContext.Bookings.Remove(booking);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Booking deleted successfully." });
        }

        private async Task ValidateBookingAsync(BookingDto bookingDto, Guid? bookingIdToExclude = null)
        {
            var venue = await dbContext.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == bookingDto.VenueId);

            if (venue == null)
            {
                ModelState.AddModelError(nameof(bookingDto.VenueId), "Please select a valid venue.");
            }
            else if (venue.Availability != VenueAvailabilityCatalog.Available)
            {
                ModelState.AddModelError(nameof(bookingDto.VenueId), "Only venues marked as available can receive new bookings.");
            }

            if (!await dbContext.Events.AnyAsync(e => e.Id == bookingDto.EventId))
            {
                ModelState.AddModelError(nameof(bookingDto.EventId), "Please select a valid event.");
            }

            var conflictingBookingExists = await dbContext.Bookings.AnyAsync(b =>
                b.VenueId == bookingDto.VenueId &&
                b.Id != bookingIdToExclude &&
                b.BookingDate.Date == bookingDto.BookingDate.Date);

            if (conflictingBookingExists)
            {
                ModelState.AddModelError(string.Empty, "This venue is already booked for the selected date.");
            }
        }
    }
}
