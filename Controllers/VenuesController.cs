using Booking_webapp.Data;
using Booking_webapp.Models;
using Booking_webapp.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("{id}")]
        public ActionResult GetVenue(int id) { 
            var venue = dbContext.Venues.Find(id);

            if (venue == null)
            {
                throw new KeyNotFoundException($"Venue with ID {id} was not found.");
            }

            return Ok(venue);
        }

        [HttpGet]
        public IActionResult GetAllVenues()
        {
            var venues = dbContext.Venues.ToList();

            return Ok(venues);
        }

        [HttpPost]
        public IActionResult PostVenue(VenueDto venueDto)
        {
            var venue = new Venue()
            {
                Name = venueDto.Name,
                Location = venueDto.Location,
                Capacity = venueDto.Capacity,
                ImageUrl = venueDto.ImageUrl
            };

            dbContext.Venues.Add(venue);
            dbContext.SaveChanges();

            return Ok(venue);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateVenue(int id, VenueDto venueDto)
        {
            var venue = dbContext.Venues.Find(id);

            if (venue == null)
            {
                throw new KeyNotFoundException($"Venue with ID {id} was not found.");
            }

            venue.Name = venueDto.Name;
            venue.Location = venueDto.Location;
            venue.Capacity = venueDto.Capacity;
            venue.ImageUrl = venueDto.ImageUrl;

            dbContext.SaveChanges();

            return Ok(venue);
        }
    }
}
