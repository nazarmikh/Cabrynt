using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cabrynt.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameRideServiceTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreferredVehicleType",
                table: "Rides",
                newName: "PreferredServiceTier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreferredServiceTier",
                table: "Rides",
                newName: "PreferredVehicleType");
        }
    }
}
