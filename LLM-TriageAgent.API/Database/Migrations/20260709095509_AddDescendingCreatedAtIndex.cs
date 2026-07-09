using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLM_TriageAgent.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDescendingCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAt_Descending",
                table: "SupportTickets_Final_v6",
                column: "CreatedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_CreatedAt_Descending",
                table: "SupportTickets_Final_v6");
        }
    }
}
