using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Booking_webapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        public readonly ApplicationDbContext dbContext;

        public VenuesController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetVenue(Guid id)
        {
            var venue = await dbContext.Venues.FindAsync(id);

            if (venue == null)
            {
                return NotFound(new { message = "The requested venue was not found." });
            }

            return Ok(venue);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVenues(string? availability = null)
        {
            var venues = dbContext.Venues.AsQueryable();

            if (!string.IsNullOrWhiteSpace(availability))
            {
                venues = venues.Where(venue => venue.Availability == availability);
            }

            return Ok(await venues.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostVenue(VenueDto venueDto)
        {
            ValidateAvailability(venueDto.Availability);

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var venue = new Venue
            {
                Name = venueDto.Name,
                Location = venueDto.Location,
                Capacity = venueDto.Capacity,
                Availability = venueDto.Availability,
                ImageUrl = venueDto.ImageUrl
            };

            dbContext.Venues.Add(venue);
            await dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVenue), new { id = venue.Id }, venue);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateVenue(Guid id, VenueDto venueDto)
        {
            var venue = await dbContext.Venues.FindAsync(id);

            if (venue == null)
            {
                return NotFound(new { message = "The requested venue was not found." });
            }

            ValidateAvailability(venueDto.Availability);

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            venue.Name = venueDto.Name;
            venue.Location = venueDto.Location;
            venue.Capacity = venueDto.Capacity;
            venue.Availability = venueDto.Availability;
            venue.ImageUrl = venueDto.ImageUrl;

            await dbContext.SaveChangesAsync();

            return Ok(venue);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteVenue(Guid id)
        {
            var venue = await dbContext.Venues.FindAsync(id);

            if (venue == null)
            {
                return NotFound(new { message = "The requested venue was not found." });
            }

            var hasBookings = await dbContext.Bookings.AnyAsync(b => b.VenueId == id);

            if (hasBookings)
            {
                return Conflict(new { message = "This venue cannot be deleted because it is linked to existing bookings." });
            }

            dbContext.Venues.Remove(venue);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Venue deleted successfully." });
        }

        private void ValidateAvailability(string availability)
        {
            if (!VenueAvailabilityCatalog.All.Contains(availability))
            {
                ModelState.AddModelError(nameof(VenueDto.Availability), "Please select a valid availability status.");
            }
        }
    }
}
