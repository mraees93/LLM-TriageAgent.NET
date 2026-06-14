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
            // 🛡️ RE-DEPLOYMENT SHIELD: Wipes out conflicting loose structures in the cloud first!
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"SupportTickets\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS triage.\"SupportTickets\";");

            migrationBuilder.EnsureSchema(
                name: "triage");

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                schema: "triage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedLabel = table.Column<string>(type: "TEXT", nullable: true),
                    AgentReply = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
