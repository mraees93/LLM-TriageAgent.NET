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

    [HttpGet]
    public async Task<IActionResult> GetAllTickets()
    {
        var tickets = await _context.SupportTickets
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            
        return Ok(tickets);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid().ToString(), 
            Title = dto.Title,
            Description = dto.Description,
            Status = "Pending"
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        await _publishEndpoint.Publish(ticket);

        return Ok(ticket);
    }

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

        ticket.Title = dto.Title;
        ticket.Description = dto.Description;
        
        ticket.Status = "Pending";
        ticket.AssignedLabel = null;
        ticket.AgentReply = null;
        ticket.CreatedAt = DateTime.UtcNow; 
        ticket.ResolvedAt = null;

        await _context.SaveChangesAsync();

        // Push back onto MassTransit event bus to awake the background worker
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