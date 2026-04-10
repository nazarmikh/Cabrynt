using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace exam_project_backend_NazarMikhin.Migrations
{
    /// <inheritdoc />
    public partial class InitOrUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Rides"
                DROP CONSTRAINT IF EXISTS "FK_Rides_Users_PassengerId";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Tickets"
                DROP CONSTRAINT IF EXISTS "FK_Tickets_Users_PassengerId";
                """);

            migrationBuilder.Sql("""
            ALTER TABLE "Users"
            DROP CONSTRAINT IF EXISTS "CK_Passenger_Points";
            """);

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HomeAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentMethod",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "Tickets",
                newName: "PassengerProfileUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_PassengerId",
                table: "Tickets",
                newName: "IX_Tickets_PassengerProfileUserId");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "Rides",
                newName: "PassengerProfileUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Rides_PassengerId",
                table: "Rides",
                newName: "IX_Rides_PassengerProfileUserId");

            migrationBuilder.CreateTable(
                name: "PassengerProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    HomeAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    PreferredPaymentMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassengerProfiles", x => x.UserId);
                    table.CheckConstraint("CK_Passenger_Points", "\"Points\" >= 0");
                    table.ForeignKey(
                        name: "FK_PassengerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_PassengerProfiles_PassengerProfileUserId",
                table: "Rides",
                column: "PassengerProfileUserId",
                principalTable: "PassengerProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_PassengerProfiles_PassengerProfileUserId",
                table: "Tickets",
                column: "PassengerProfileUserId",
                principalTable: "PassengerProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rides_PassengerProfiles_PassengerProfileUserId",
                table: "Rides");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_PassengerProfiles_PassengerProfileUserId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "PassengerProfiles");

            migrationBuilder.RenameColumn(
                name: "PassengerProfileUserId",
                table: "Tickets",
                newName: "PassengerId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_PassengerProfileUserId",
                table: "Tickets",
                newName: "IX_Tickets_PassengerId");

            migrationBuilder.RenameColumn(
                name: "PassengerProfileUserId",
                table: "Rides",
                newName: "PassengerId");

            migrationBuilder.RenameIndex(
                name: "IX_Rides_PassengerProfileUserId",
                table: "Rides",
                newName: "IX_Rides_PassengerId");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Users",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HomeAddress",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredPaymentMethod",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Passenger_Points",
                table: "Users",
                sql: "\"Points\" >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_PassengerId",
                table: "Rides",
                column: "PassengerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_PassengerId",
                table: "Tickets",
                column: "PassengerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
