using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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


        [HttpGet("{id}")]
        public ActionResult GetBooking(int id)
        {
            var booking = dbContext.Bookings.Find(id);

            if (booking == null)
            {
                throw new KeyNotFoundException($"Booking with ID {id} was not found.");
            }

            return Ok(booking);
        }

        [HttpGet]
        public IActionResult GetAllBookings()
        {
            var booking = dbContext.Bookings.ToList();

            return Ok(booking);
        }

        [HttpPost]
        public IActionResult PostBooking(BookingDto bookingDto)
        {
            var booking = new Booking()
            {
                BookingDate = bookingDto.BookingDate,
                Status = bookingDto.Status
            };

            dbContext.Bookings.Add(booking);
            dbContext.SaveChanges();

            return Ok(booking);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateBooking(int id, BookingDto bookingDto)
        {
            var booking = dbContext.Bookings.Find(id);

            if (booking == null)
            {
                throw new KeyNotFoundException($"Booking with ID {id} was not found.");
            }

            booking.BookingDate = bookingDto.BookingDate;
            booking.Status = bookingDto.Status;

            dbContext.SaveChanges();

            return Ok(booking);
        }
    }
}
