using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Checklist.Api.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            ;WITH d AS (
            SELECT
                Id,
                ExecutionId,
                ROW_NUMBER() OVER (PARTITION BY ExecutionId ORDER BY DecidedAt DESC, Id DESC) AS rn
            FROM Approvals
            )
            DELETE A
            FROM Approvals A
            JOIN d ON d.Id = A.Id
            WHERE d.rn > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "UX_Approval_Execution",
                table: "Approvals",
                column: "ExecutionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Approval_Execution",
                table: "Approvals");
        }
    }
}