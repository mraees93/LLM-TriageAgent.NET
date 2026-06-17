using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;
using LLM_TriageAgent.API.Utils;

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
        Console.WriteLine($"[Queue Consumer] Picked up Ticket #{queuedTicket.Id} from the message queue.");

        var dbTicket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == queuedTicket.Id);
        
        if (dbTicket == null || dbTicket.Status == "Resolved" || dbTicket.Status == "Processing")
        {
            return; // IDEMPOTENCY SHIELD: Guard against duplicate processing loops
        }

        dbTicket.Status = "Processing";
        await _dbContext.SaveChangesAsync();

        int? httpErrorCode = TriageUtilities.ExtractHttpErrorCode(dbTicket.Title, dbTicket.Description);

        bool isProductionCloud = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

        if (isProductionCloud)
        {
            try
            {
                Console.WriteLine("[Cloud AI Agent] Emulating background tool analysis loops...");
                
                int dynamicDelayMs = new Random().Next(2500, 7500);
                await Task.Delay(dynamicDelayMs);

                var (mockLabel, mockReply) = TriageUtilities.GetProductionMockResponse(httpErrorCode, dbTicket.Description);

                dbTicket.AssignedLabel = mockLabel;
                dbTicket.AgentReply = mockReply;
                dbTicket.Status = "Resolved";
                dbTicket.ResolvedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"[Cloud AI Agent] Successfully resolved ticket #{dbTicket.Id}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Cloud Queue Failure] Exception trace loop triggered: {ex.Message}");
                throw;
            }
        }
        else
        {
            // LOCAL DEVELOPMENT OLLAMA HARDWARE CORE

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var ollamaUrl = "http://localhost:11434/api/generate";

            try
            {
                string promptTemplate = TriageUtilities.GenerateAiPrompt(httpErrorCode, dbTicket.Title, dbTicket.Description);

                var requestBody = new { model = "phi3", prompt = promptTemplate, stream = false };
                var response = await client.PostAsJsonAsync(ollamaUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var jsonResult = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                string rawAiResponse = jsonResult?.Response ?? "";

                dbTicket.AssignedLabel = rawAiResponse.ToLower().Contains("bug") ? "bug" : "investigate";
                dbTicket.AgentReply = $"LOCAL OLLAMA PHI3 RUNTIME INFERENCE: {rawAiResponse}";
                dbTicket.Status = "Resolved";
                dbTicket.ResolvedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"[Local AI Agent] Successfully processed ticket #{dbTicket.Id}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Local Core Crash] Ollama connection wall hit: {ex.Message}");
                throw;
            }
        }
    }

    private class OllamaResponse { public string Response { get; set; } = ""; }
}
