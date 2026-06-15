using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LLM_TriageAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public TicketsController(AppDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    // 1. GET: Fetch all records from our new table layout
    [HttpGet]
    public async Task<IActionResult> GetAllTickets()
    {
        var tickets = await _context.SupportTickets
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            
        return Ok(tickets);
    }

    // 2. POST: Publish a new ticket to the event bus (Matches your plain string post layout style!)
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid().ToString(), // Assigns a secure string identifier key
            Title = dto.Title,
            Description = dto.Description,
            Status = "Pending"
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        await _publishEndpoint.Publish(ticket);

        return Ok(ticket);
    }

    // 3. DELETE: Clear a ticket off the dashboard layout (DELETE api/tickets/{id})
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(string id)
    {
        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound(new { message = "Link not found or already deleted" });
        }

        _context.SupportTickets.Remove(ticket);
        await _context.SaveChangesAsync();
        
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(string id, [FromBody] UpdateTicketDto dto)
    {
        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound(new { message = "Target record not found or already deleted." });
        }

        // Overwrite text inputs while preserving the exact same database ID!
        ticket.Title = dto.Title;
        ticket.Description = dto.Description;
        
        // Reset status properties so the AI agent re-evaluates the fresh text data!
        ticket.Status = "Pending";
        ticket.AssignedLabel = null;
        ticket.AgentReply = null;
        ticket.CreatedAt = DateTime.UtcNow; 
        ticket.ResolvedAt = null;

        await _context.SaveChangesAsync();

        // 🚀 Push back onto MassTransit event bus to awake the background worker!
        await _publishEndpoint.Publish(ticket);

        return Ok(ticket);
    }
}

public class CreateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}