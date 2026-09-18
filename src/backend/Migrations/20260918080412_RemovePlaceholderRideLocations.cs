using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cabrynt.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlaceholderRideLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartureLocation",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DestinationLocation",
                table: "Rides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartureLocation",
                table: "Rides",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DestinationLocation",
                table: "Rides",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
