using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatbot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder
        )
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "Leads",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "ContactedAtUtc",
                table: "Leads",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "StaffNotes",
                table: "Leads",
                type: "TEXT",
                maxLength: 2000,
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Leads",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.Sql(
                """
                UPDATE "Leads"
                SET "UpdatedAtUtc" = "CreatedAtUtc"
                WHERE "UpdatedAtUtc" IS NULL;
                """
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Leads",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CreatedAtUtc",
                table: "Leads",
                column: "CreatedAtUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                table: "Leads",
                column: "Status"
            );
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder
        )
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_CreatedAtUtc",
                table: "Leads"
            );

            migrationBuilder.DropIndex(
                name: "IX_Leads_Status",
                table: "Leads"
            );

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "Leads"
            );

            migrationBuilder.DropColumn(
                name: "ContactedAtUtc",
                table: "Leads"
            );

            migrationBuilder.DropColumn(
                name: "StaffNotes",
                table: "Leads"
            );

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Leads"
            );
        }
    }
}