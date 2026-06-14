using Microsoft.EntityFrameworkCore;
using LLM_TriageAgent.API.Models;

namespace LLM_TriageAgent.API.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 🛡️ MULTI-PROJECT ISOLATION SHIELD
        // This forces this project to only talk to a separate schema named "triage"
        // Other portfolio project tables are never touched or overwritten!
        modelBuilder.HasDefaultSchema("triage");

        // Ticket ID must be unique
        modelBuilder.Entity<SupportTicket>()
            .HasKey(t => t.Id);
    }
}
