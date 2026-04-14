using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace exam_project_backend_NazarMikhin.Migrations
{
    /// <inheritdoc />
    public partial class AddRideCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DepartureLatitude",
                table: "Rides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DepartureLongitude",
                table: "Rides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLatitude",
                table: "Rides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLongitude",
                table: "Rides",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartureLatitude",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DepartureLongitude",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DestinationLatitude",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DestinationLongitude",
                table: "Rides");
        }
    }
}
