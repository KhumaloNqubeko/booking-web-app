namespace Booking_webapp.Models.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid VenueId { get; set; }

        public Guid EventId { get; set; }

        public DateTime BookingDate { get; set; }
        public string Status { get; set; }

    }
}
