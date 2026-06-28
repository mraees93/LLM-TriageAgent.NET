using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MassTransit;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LLM_TriageAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMemoryCache _memoryCache;
    private const string CacheKey = "AllTickets_Cache_Key";

    public TicketsController(AppDbContext context, IPublishEndpoint publishEndpoint, IMemoryCache memoryCache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _memoryCache = memoryCache;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTickets()
    {
        if (!_memoryCache.TryGetValue(CacheKey, out List<SupportTicket>? tickets))
        {
            tickets = await _context.SupportTickets
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(5));

            _memoryCache.Set(CacheKey, tickets, cacheOptions);
        }
            
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

        _memoryCache.Remove(CacheKey);

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

        _memoryCache.Remove(CacheKey);
        
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

        _memoryCache.Remove(CacheKey);

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
