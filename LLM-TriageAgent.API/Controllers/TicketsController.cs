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

    // 1. GET: Fetch all records from our isolated schema
    [HttpGet]
    public async Task<IActionResult> GetAllTickets()
    {
        var tickets = await _context.SupportTickets
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            
        return Ok(tickets);
    }

    // 2. POST: Publish a new ticket to the event bus
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        // Flexible string parsing safely prevents 500 mapping traps on PostgreSQL
        Guid targetGuid = (!string.IsNullOrEmpty(dto.Id) && Guid.TryParse(dto.Id, out var parsedGuid)) 
            ? parsedGuid 
            : Guid.NewGuid();

        // Gateway Idempotency Guard
        var existingTicket = await _context.SupportTickets.AnyAsync(t => t.Id == targetGuid);
        if (existingTicket)
        {
            return Ok(new { TicketId = targetGuid, Message = "Ticket is already registered." });
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

            return Accepted(new { TicketId = ticket.Id, Message = "Ticket received and queued." });
        }
        catch (DbUpdateException)
        {
            return Ok(new { TicketId = ticket.Id, Message = "Transaction clash detected." });
        }
    }

    // 3. DELETE: Clear a ticket off the dashboard metrics layout
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null)
        {
            return NotFound(new { Message = "Target record not found in schema context." });
        }

        _context.SupportTickets.Remove(ticket);
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Ticket {id} successfully purged from database context." });
    }
}

// Robust, cloud-optimized DTO Landing Carrier
public class CreateTicketDto
{
    public string? Id { get; set; } 
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
