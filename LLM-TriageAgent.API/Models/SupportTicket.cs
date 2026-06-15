using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLM_TriageAgent.API.Models;

public class SupportTicket
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending";
    
    public string? AssignedLabel { get; set; }
    public string? AgentReply { get; set; }
    
    // Telemetry Timestamps: Explicitly handles both SQLite and PostgreSQL perfectly
    [Column(TypeName = "timestamp with time zone")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "timestamp with time zone")]
    public DateTime? ResolvedAt { get; set; } 
}
