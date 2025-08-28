using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Checklist.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueActiveExecutionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            ;WITH x AS (
            SELECT Id, VehicleId, StartedAt,
                    ROW_NUMBER() OVER (PARTITION BY VehicleId ORDER BY ISNULL(StartedAt,'19000101') DESC, Id) AS rn
            FROM dbo.Executions
            WHERE [Status] IN (0,1) AND ReferenceDate IS NULL
            )
            UPDATE e
            SET ReferenceDate = DATEADD(day, x.rn - 1, CAST(GETUTCDATE() AS date))
            FROM dbo.Executions e
            JOIN x ON x.Id = e.Id;
            ");

            migrationBuilder.Sql(@"
            WHILE EXISTS (
            SELECT 1
            FROM dbo.Executions
            WHERE [Status] IN (0,1) AND ReferenceDate IS NOT NULL
            GROUP BY VehicleId, ReferenceDate
            HAVING COUNT(*) > 1
            )
            BEGIN
            ;WITH dupe AS (
                SELECT e.Id, e.VehicleId, e.ReferenceDate,
                    ROW_NUMBER() OVER (PARTITION BY e.VehicleId, e.ReferenceDate
                                        ORDER BY ISNULL(e.StartedAt,'19000101') DESC, e.Id) AS rn
                FROM dbo.Executions e
                WHERE e.[Status] IN (0,1) AND e.ReferenceDate IS NOT NULL
            )
            UPDATE e
            SET ReferenceDate = DATEADD(day, 1, d.ReferenceDate)
            FROM dbo.Executions e
            JOIN dupe d ON e.Id = d.Id
            WHERE d.rn > 1;
            END
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UniqueActiveExecution' AND object_id = OBJECT_ID('dbo.Executions'))
                DROP INDEX IX_UniqueActiveExecution ON dbo.Executions;
            ");

            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ActiveExecution_Vehicle_Date' AND object_id = OBJECT_ID('dbo.Executions'))
                CREATE UNIQUE INDEX UX_ActiveExecution_Vehicle_Date
                ON dbo.Executions (VehicleId, ReferenceDate)
                WHERE [Status] IN (0,1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ActiveExecution_Vehicle_Date",
                table: "Executions");

            migrationBuilder.CreateIndex(
                name: "IX_UniqueActiveExecution",
                table: "Executions",
                columns: new[] { "VehicleId", "ReferenceDate", "Status" },
                filter: "[Status] IN (0,1)");
        }
    }
}
