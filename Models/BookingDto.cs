namespace Booking_webapp.Models
{
    public class BookingDto
    {
        public Guid VenueId { get; set; }
        public Guid EventId { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
