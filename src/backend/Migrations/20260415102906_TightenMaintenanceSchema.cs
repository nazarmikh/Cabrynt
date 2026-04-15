using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace exam_project_backend_NazarMikhin.Migrations
{
    /// <inheritdoc />
    public partial class TightenMaintenanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'TechnicianName'
                    ) THEN
                        UPDATE "Maintenances"
                        SET "TechnicianName" = ''
                        WHERE "TechnicianName" IS NULL;

                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "TechnicianName" TYPE character varying(200);

                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "TechnicianName" SET NOT NULL;
                    ELSE
                        ALTER TABLE "Maintenances"
                        ADD "TechnicianName" character varying(200) NOT NULL DEFAULT '';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileage'
                    ) THEN
                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "NextInspectionMileage" TYPE integer
                        USING ROUND("NextInspectionMileage")::integer;
                    ELSE
                        ALTER TABLE "Maintenances"
                        ADD "NextInspectionMileage" integer NOT NULL DEFAULT 1;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'Description'
                    ) THEN
                        UPDATE "Maintenances"
                        SET "Description" = ''
                        WHERE "Description" IS NULL;

                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "Description" TYPE character varying(2000);

                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "Description" SET NOT NULL;
                    ELSE
                        ALTER TABLE "Maintenances"
                        ADD "Description" character varying(2000) NOT NULL DEFAULT '';
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'TechnicianName'
                    ) THEN
                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "TechnicianName" TYPE text;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'NextInspectionMileage'
                    ) THEN
                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "NextInspectionMileage" TYPE double precision
                        USING "NextInspectionMileage"::double precision;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'Maintenances' AND column_name = 'Description'
                    ) THEN
                        ALTER TABLE "Maintenances"
                        ALTER COLUMN "Description" TYPE text;
                    END IF;
                END $$;
                """);
        }
    }
}
