using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;

namespace LLM_TriageAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    // inject db Context and MassTransit queue engine here
    public TicketsController(AppDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var targetGuid = dto.Id ?? Guid.NewGuid();

        // 🛡️ APPLICATION LEVEL IDEMPOTENCY CHECK
        // Check if this ID already exists in our records BEFORE attempting a write
        var existingTicket = await _context.SupportTickets.AnyAsync(t => t.Id == targetGuid);
        if (existingTicket)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️ [API Gateway Guard] Blocked incoming duplicate HTTP POST request for Ticket #{targetGuid}. De-duplication successful.");
            Console.ResetColor();

            // Return a clean 200 OK or 409 Conflict instead of crashing the thread!
            return Ok(new { TicketId = targetGuid, Message = "Ticket is already registered and undergoing processing." });
        }

        var ticket = new SupportTicket
        {
            Id = targetGuid,
            Title = dto.Title,
            Description = dto.Description,
            Status = "Pending"
        };

        try
        {
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish(ticket);

            return Accepted(new { TicketId = ticket.Id, Message = "Ticket received and queued for AI analysis." });
        }
        catch (DbUpdateException)
        {
            // Fallback safety net if two requests hit at the exact same millisecond
            return Ok(new { TicketId = ticket.Id, Message = "Transaction clash detected. Item is already processing." });
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetAllTickets()
    {
        var tickets = await _context.SupportTickets
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tickets);
    }

    // TEMPORARY TEST ENDPOINT: Simulates a network duplicate retry
    [HttpPost("test-idempotency")]
    public async Task<IActionResult> TestIdempotency()
    {
        var duplicateTicket = new SupportTicket
        {
            Id = Guid.NewGuid(), // Both messages will share this exact same ID
            Title = "Idempotency Test Trigger",
            Description = "Testing 404 bug retries."
        };

        // 1. Save it once to the database
        _context.SupportTickets.Add(duplicateTicket);
        await _context.SaveChangesAsync();

        Console.WriteLine($"\n🚀 [Test Trigger] Sending Message #1 for Ticket {duplicateTicket.Id}...");
        await _publishEndpoint.Publish(duplicateTicket);

        // Simulate a network glitch by waiting 2 seconds, then sending the exact same message again!
        await Task.Delay(2000);

        Console.WriteLine($"\n🚀 [Test Trigger] Sending Duplicate Message #2 for Ticket {duplicateTicket.Id}...");
        await _publishEndpoint.Publish(duplicateTicket);

        return Ok(new { Message = "Sent normal message and duplicate retry message into the queue." });
    }

}

// Data Transfer Object to format the incoming JSON data neatly
public record CreateTicketDto(Guid? Id, string Title, string Description);