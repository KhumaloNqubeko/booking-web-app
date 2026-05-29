using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking_webapp.Migrations
{
    /// <inheritdoc />
    public partial class initialmigration : Migration
    {
        private bool IsSqlServer()
        {
            return ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var eventDateTimeType = IsSqlServer()
                ? "datetime2"
                : "timestamp without time zone";

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Location = table.Column<string>(nullable: false),
                    Capacity = table.Column<int>(nullable: false),
                    ImageUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    StartDateTime = table.Column<DateTime>(type: eventDateTimeType, nullable: false),
                    EndDateTime = table.Column<DateTime>(type: eventDateTimeType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    VenueId = table.Column<Guid>(nullable: false),
                    EventId = table.Column<Guid>(nullable: false),
                    BookingDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Venues");
        }
    }
}
