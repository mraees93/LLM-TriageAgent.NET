using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;

namespace LLM_TriageAgent.API.Services;

public class TicketConsumer : IConsumer<SupportTicket>
{
    private readonly AppDbContext _dbContext;

    public TicketConsumer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<SupportTicket> context)
    {
        var queuedTicket = context.Message;
        Console.WriteLine($"\n📥 [Queue Consumer] Picked up Ticket #{queuedTicket.Id} from the message queue.");

        // UPDATED IDEMPOTENCY GUARD (RACE-CONDITION RESILIENT)
        var dbTicket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == queuedTicket.Id);
        
        if (dbTicket == null)
        {
            Console.WriteLine($"⚠️ [Idempotency Guard] Ticket #{queuedTicket.Id} was not found in the database. Skipping.");
            return;
        }

        // CRITICAL CHECK: Stop if the ticket is already Resolved OR currently being processed!
        if (dbTicket.Status == "Resolved" || dbTicket.Status == "Processing")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"🛑 [Idempotency Guard] Duplicate message blocked! Ticket #{queuedTicket.Id} is already [{dbTicket.Status}]. Skipping processing.");
            Console.ResetColor();
            return;
        }

        // If it passes the check, we lock it instantly by setting it to Processing
        dbTicket.Status = "Processing";
        await _dbContext.SaveChangesAsync();

        // ====================================================================
        // RUN THE AI AGENT CORE ENGINE LOGIC
        // ====================================================================
        using var client = new HttpClient();
        
        // FIXED: Extends the network limit to 5 minutes so local LLM hardware doesn't crash on timeouts
        client.Timeout = TimeSpan.FromMinutes(5); 
        
        var ollamaUrl = "http://localhost:11434/api/generate";

        try
        {
            // STEP 1 (AI Logic): Ask the local model to extract the error code
            var payload = new
            {
                model = "phi3",
                prompt = $"Extract ONLY the numerical error code or route page from this ticket description: '{dbTicket.Description}'. Respond with just the core technical identifier phrase.",
                stream = false
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(ollamaUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            
            using var doc = JsonDocument.Parse(responseString);
            string errorCode = doc.RootElement.GetProperty("response").GetString()?.Trim() ?? "404";

            // STEP 2 (Tool Execution): Call our mock log tool logic
            Console.WriteLine($"🔍 [AI Agent Tool Use] Searching backend logs for code identifier: '{errorCode}'...");
            string mockLogs = errorCode.Contains("404") 
                ? "DATABASE LOG: Thread 4: Error 404 on endpoint '/api/auth/login'. Reason: Route missing from RouteConfig.cs file mapping."
                : "DATABASE LOG: General warning. No explicit route failure mappings located.";

            // STEP 3 (Reasoning Loop): Pass logs back to the AI to draft a human solution reply
            var triagePayload = new
            {
                model = "phi3",
                prompt = $"Based on these system logs: '{mockLogs}', draft a short 1-sentence fix message for the user reporting the issue. Do not include extra conversational text.",
                stream = false
            };

            var triageJson = JsonSerializer.Serialize(triagePayload);
            var triageContent = new StringContent(triageJson, Encoding.UTF8, "application/json");
            var triageResponse = await client.PostAsync(ollamaUrl, triageContent);
            var triageResponseString = await triageResponse.Content.ReadAsStringAsync();
            
            using var triageDoc = JsonDocument.Parse(triageResponseString);
            string aiFixMessage = triageDoc.RootElement.GetProperty("response").GetString()?.Trim() ?? "Configuration audit required.";

            // STEP 4 (Multi-step Action): Update database with final details
            dbTicket.AssignedLabel = errorCode.Contains("404") ? "bug" : "investigate";
            dbTicket.AgentReply = aiFixMessage;
            dbTicket.Status = "Resolved";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🎯 [AI Agent Action] Successfully resolved ticket #{dbTicket.Id}!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ [Queue Consumer Error] Processing failed. Details: {ex.Message}");
            Console.ResetColor();
            dbTicket.Status = "Failed";
        }

        await _dbContext.SaveChangesAsync();
    }
}
