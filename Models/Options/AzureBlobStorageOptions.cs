namespace Booking_webapp.Models.Options
{
    public class AzureBlobStorageOptions
    {
        public const string SectionName = "AzureBlobStorage";

        public string StorageConnection { get; set; } = string.Empty;
        public string VenueContainerName { get; set; } = "venue-images";
        public string EventContainerName { get; set; } = "event-images";
    }
}
