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

    // We inject our Database Context and MassTransit queue engine here
    public TicketsController(AppDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    // ENDPOINT 1: Submit a new ticket (POST api/tickets)
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var ticket = new SupportTicket
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = "Pending"
        };

        // 1. Save the incoming ticket into your SQLite database
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        // 2. Multi-step Workflow Trigger: Drop the ticket onto the background queue!
        // This instantly frees up our Web API so the front-end never has to wait.
        await _publishEndpoint.Publish(ticket);

        // 3. Return a 202 Accepted response along with the generated Ticket GUID
        return Accepted(new { TicketId = ticket.Id, Message = "Ticket received and queued for AI analysis." });
    }

    // ENDPOINT 2: Fetch all tickets for our UI list (GET api/tickets)
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

// A simple Data Transfer Object to format the incoming JSON data neatly
public record CreateTicketDto(string Title, string Description);
