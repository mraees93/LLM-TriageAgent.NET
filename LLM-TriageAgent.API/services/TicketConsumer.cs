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

    // Inject our database context so the worker can update ticket statuses
    public TicketConsumer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<SupportTicket> context)
    {
        var queuedTicket = context.Message;
        Console.WriteLine($"\n📥 [Queue Consumer] Picked up Ticket #{queuedTicket.Id} from the message queue!");

        // 1. Fetch the ticket from the real database to update its status to Processing
        var dbTicket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == queuedTicket.Id);
        if (dbTicket == null) return;

        dbTicket.Status = "Processing";
        await _dbContext.SaveChangesAsync();

        // 2. Setup standard .NET HttpClient to speak with your local Ollama server
        using var client = new HttpClient();
        var ollamaUrl = "http://localhost:11434/api/generate";

        try
        {
            // 3. STEP 1 (AI Logic): Ask the local model to extract the error code
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

            // 4. STEP 2 (Tool Execution): Call our mock log tool logic
            Console.WriteLine($"🔍 [AI Agent Tool Use] Searching backend logs for code identifier: '{errorCode}'...");
            string mockLogs = "";
            if (errorCode.Contains("404"))
            {
                mockLogs = "DATABASE LOG: Thread 4: Error 404 on endpoint '/api/auth/login'. Reason: Route missing from RouteConfig.cs file mapping.";
            }
            else
            {
                mockLogs = "DATABASE LOG: General warning. No explicit route failure mappings located.";
            }

            // 5. STEP 3 (Reasoning Loop): Pass logs back to the AI to draft a human solution reply
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

            // 6. STEP 4 (Multi-step Action): Update database with the finalized resolution details
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
            Console.WriteLine($"❌ [Queue Consumer Error] Processing failed. Ensure Ollama is open! Details: {ex.Message}");
            Console.ResetColor();
            dbTicket.Status = "Failed";
        }

        // Save all changes permanently to your SQLite database file
        await _dbContext.SaveChangesAsync();
    }
}
