namespace Booking_webapp.Models
{
    public class VenueDto
    {
        public required string Name { get; set; }
        public required string Location { get; set; }
        public required int Capacity { get; set; }
        public string Availability { get; set; } = VenueAvailabilityCatalog.Available;
        public string? ImageUrl { get; set; }
    }
}
