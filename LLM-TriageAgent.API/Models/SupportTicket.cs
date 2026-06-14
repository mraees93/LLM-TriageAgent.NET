using System;

namespace LLM_TriageAgent.API.Models;

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending";
    
    public string? AssignedLabel { get; set; }
    public string? AgentReply { get; set; }
    
    // Telemetry Timestamps: Used to calculate queue resolution processing latency!
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ✅ ADDED BACK: This is the missing property the compiler is looking for!
    public DateTime? ResolvedAt { get; set; } 
}
