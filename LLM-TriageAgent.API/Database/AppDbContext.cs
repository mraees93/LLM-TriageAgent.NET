using Microsoft.EntityFrameworkCore;
using LLM_TriageAgent.API.Models;

namespace LLM_TriageAgent.API.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // This creates our "SupportTickets" table in the database
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tell the database that Ticket ID must be unique
        modelBuilder.Entity<SupportTicket>()
            .HasKey(t => t.Id);
    }
}
