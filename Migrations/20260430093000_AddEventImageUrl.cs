using System;
using Booking_webapp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Booking_webapp.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260430093000_AddEventImageUrl")]
    public class AddEventImageUrl : Migration
    {
        private bool IsSqlServer()
        {
            return (ActiveProvider ?? string.Empty).Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Events",
                type: IsSqlServer() ? "nvarchar(max)" : "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Events");
        }
    }
}
