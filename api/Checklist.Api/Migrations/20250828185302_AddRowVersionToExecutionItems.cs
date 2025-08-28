using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Checklist.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToExecutionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateItems_Templates_ChecklistTemplateId",
                table: "TemplateItems");

            migrationBuilder.DropIndex(
                name: "IX_TemplateItems_ChecklistTemplateId",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "ChecklistTemplateId",
                table: "TemplateItems");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ExecutionItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateItems_Templates_TemplateId",
                table: "TemplateItems",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateItems_Templates_TemplateId",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ExecutionItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ChecklistTemplateId",
                table: "TemplateItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_ChecklistTemplateId",
                table: "TemplateItems",
                column: "ChecklistTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateItems_Templates_ChecklistTemplateId",
                table: "TemplateItems",
                column: "ChecklistTemplateId",
                principalTable: "Templates",
                principalColumn: "Id");
        }
    }
}
