using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;

namespace LLM_TriageAgent.API.Services;

// 🔔 DEAD LETTER FAULT CONSUMER: Intercepts background crashes automatically!
public class TicketFaultConsumer : IConsumer<Fault<SupportTicket>>
{
    private readonly AppDbContext _dbContext;

    public TicketFaultConsumer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<Fault<SupportTicket>> context)
    {
        // Extract the original support ticket payload from inside the fault message
        var originalTicket = context.Message.Message;
        Console.WriteLine($"⚠️ [Dead Letter Queue] Intercepted system crash loop for ticket #{originalTicket.Id}");

        // Find the record in our flat Vertex database layout table
        var dbTicket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == originalTicket.Id);
        if (dbTicket == null) return;

        // 🛡️ SELF-HEALING ACTION: Safeguard the card layout from getting stuck in an infinite loop!
        dbTicket.Status = "Failed";
        dbTicket.AssignedLabel = "SYSTEM_FAULT";
        dbTicket.AgentReply = "DEAD LETTER QUEUE EXCEPTION LOG: Background queue consumer encountered a critical hardware connection exception. Ticket has been safely quarantined for engineering review.";

        await _dbContext.SaveChangesAsync();
    }
}
