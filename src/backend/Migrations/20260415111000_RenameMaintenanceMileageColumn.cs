using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Project.Data;

namespace exam_project_backend_NazarMikhin.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260415111000_RenameMaintenanceMileageColumn")]
    public partial class RenameMaintenanceMileageColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileaga'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileage'
                    ) THEN
                        ALTER TABLE "Maintenances"
                        RENAME COLUMN "NextInspectionMileaga" TO "NextInspectionMileage";
                    ELSIF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileaga'
                    ) AND EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileage'
                    ) THEN
                        UPDATE "Maintenances"
                        SET "NextInspectionMileage" = COALESCE("NextInspectionMileage", ROUND("NextInspectionMileaga")::integer);

                        ALTER TABLE "Maintenances"
                        DROP COLUMN "NextInspectionMileaga";
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileage'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileaga'
                    ) THEN
                        ALTER TABLE "Maintenances"
                        RENAME COLUMN "NextInspectionMileage" TO "NextInspectionMileaga";
                    END IF;
                END $$;
                """);
        }
    }
}
