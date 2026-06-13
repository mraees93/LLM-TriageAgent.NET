using System;

namespace LLM_TriageAgent.API.Models;

public class SupportTicket
{
    // The unique ID for the ticket (Crucial for our Idempotency checks!)
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Tracking statuses: Pending, Processing, Resolved, Failed
    public string Status { get; set; } = "Pending";
    
    // Fields that our AI Agent will fill in autonomously later
    public string? AssignedLabel { get; set; }
    public string? AgentReply { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
