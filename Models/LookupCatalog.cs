namespace Booking_webapp.Models
{
    public static class EventTypeCatalog
    {
        public const int Conference = 1;
        public const int Wedding = 2;
        public const int Concert = 3;
        public const int Seminar = 4;
        public const int Workshop = 5;
        public const int Birthday = 6;
        public const int CorporateEvent = 7;
        public const int Exhibition = 8;

        public static IReadOnlyList<EventTypeCatalogItem> All { get; } =
        [
            new(Conference, "Conference"),
            new(Wedding, "Wedding"),
            new(Concert, "Concert"),
            new(Seminar, "Seminar"),
            new(Workshop, "Workshop"),
            new(Birthday, "Birthday"),
            new(CorporateEvent, "Corporate Event"),
            new(Exhibition, "Exhibition")
        ];
    }

    public static class VenueAvailabilityCatalog
    {
        public const string Available = "Available";
        public const string Unavailable = "Unavailable";

        public static IReadOnlyList<string> All { get; } =
        [
            Available,
            Unavailable
        ];
    }

    public static class BookingStatusCatalog
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";

        public static IReadOnlyList<string> All { get; } =
        [
            Pending,
            Confirmed,
            Cancelled
        ];
    }

    public static class SearchScopeCatalog
    {
        public const string All = "All";
        public const string Venues = "Venues";
        public const string Events = "Events";
        public const string Bookings = "Bookings";

        public static IReadOnlyList<string> AllValues { get; } =
        [
            All,
            Venues,
            Events,
            Bookings
        ];
    }

    public sealed record EventTypeCatalogItem(int Id, string Name);
}
