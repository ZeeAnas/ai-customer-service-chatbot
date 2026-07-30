using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatbot.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureLeadCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Leads",
                type: "TEXT",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "Leads",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ConversationId",
                table: "Leads",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Conversations_ConversationId",
                table: "Leads",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Conversations_ConversationId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ConversationId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Leads");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Leads",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 254,
                oldNullable: true);
        }
    }
}
