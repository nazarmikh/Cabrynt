using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace exam_project_backend_NazarMikhin.Migrations
{
    /// <inheritdoc />
    public partial class RidePriceCalculationData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DestinationLocation",
                table: "Rides",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DepartureLocation",
                table: "Rides",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "DiscountCodeId",
                table: "Rides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Rides",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Duration",
                table: "Rides",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPrice",
                table: "Rides",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PreferredVehicleType",
                table: "Rides",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumRideValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                    table.CheckConstraint("CK_DiscountCode_MinimumRideValue", "\"MinimumRideValue\" >= 0");
                    table.CheckConstraint("CK_DiscountCode_PercentageRange", "\"Type\" <> 1 OR (\"Value\" > 0 AND \"Value\" <= 100)");
                    table.CheckConstraint("CK_DiscountCode_Value", "\"Value\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_DiscountCodeId",
                table: "Rides",
                column: "DiscountCodeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ride_Distance",
                table: "Rides",
                sql: "\"Distance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ride_Duration",
                table: "Rides",
                sql: "\"Duration\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Ride_EstimatedPrice",
                table: "Rides",
                sql: "\"EstimatedPrice\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_Code",
                table: "DiscountCodes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_DiscountCodes_DiscountCodeId",
                table: "Rides",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_DiscountCodes_DiscountCodeId",
                table: "Rides");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropIndex(
                name: "IX_Rides_DiscountCodeId",
                table: "Rides");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ride_Distance",
                table: "Rides");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ride_Duration",
                table: "Rides");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Ride_EstimatedPrice",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "PreferredVehicleType",
                table: "Rides");

            migrationBuilder.AlterColumn<string>(
                name: "DestinationLocation",
                table: "Rides",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "DepartureLocation",
                table: "Rides",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
