using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cabrynt.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRideQuoteSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedTripDuration",
                table: "Rides",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedTripDurationSource",
                table: "Rides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TripDurationModelVersion",
                table: "Rides",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedTripDuration",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "EstimatedTripDurationSource",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "TripDurationModelVersion",
                table: "Rides");
        }
    }
}
