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
}

// A simple Data Transfer Object to format the incoming JSON data neatly
public record CreateTicketDto(string Title, string Description);
