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

        // to avoid database folder prefix errors on Render
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("SupportTickets_Final_v6");

            // Adds a descending B-Tree index to optimize chronologically sorted read queries
            entity.HasIndex(t => t.CreatedAt)
                  .HasDatabaseName("IX_SupportTickets_CreatedAt_Descending")
                  .IsDescending();
        });
    }
}
