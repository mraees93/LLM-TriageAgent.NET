using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LLM_TriageAgent.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialProductionSchemaSetup : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipes out conflicting loose structures in the cloud first!
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SupportTickets\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS triage.\"SupportTickets\";");

            migrationBuilder.EnsureSchema(
                name: "triage");

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                schema: "triage",
                columns: table => new
                {
                    // ✅ FIXED: Removed type: "TEXT" to allow native PostgreSQL UUID and Timestamp mapping!
                    Id = table.Column<Guid>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    AssignedLabel = table.Column<string>(nullable: true),
                    AgentReply = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ResolvedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportTickets",
                schema: "triage");
        }
    }
}
