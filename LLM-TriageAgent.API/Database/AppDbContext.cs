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
        });
    }
}
