using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cabrynt.Backend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyRideRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_DiscountCodes_DiscountCodeId",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Vehicles_VehicleId",
                table: "Rides");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Rides_DiscountCodeId",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_VehicleId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Rides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountCodeId",
                table: "Rides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "Rides",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumRideValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                    table.CheckConstraint("CK_DiscountCode_MinimumRideValue", "\"MinimumRideValue\" >= 0");
                    table.CheckConstraint("CK_DiscountCode_PercentageRange", "\"Type\" <> 1 OR (\"Value\" > 0 AND \"Value\" <= 100)");
                    table.CheckConstraint("CK_DiscountCode_Value", "\"Value\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RideId = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    PayAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TransactionStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payment_PayAmount", "\"PayAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Payments_Rides_RideId",
                        column: x => x.RideId,
                        principalTable: "Rides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DiscountCodeId",
                table: "Rides",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_VehicleId",
                table: "Rides",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_Code",
                table: "DiscountCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RideId",
                table: "Payments",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionReference",
                table: "Payments",
                column: "TransactionReference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_DiscountCodes_DiscountCodeId",
                table: "Rides",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Vehicles_VehicleId",
                table: "Rides",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
