namespace Booking_webapp.Models.Entities
{
    public class Venue
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }

        public required string Location { get; set; }
        public required int Capacity { get; set; }

        public string? ImageUrl { get; set; }
    }
}
