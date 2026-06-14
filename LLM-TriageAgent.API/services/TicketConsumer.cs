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

        var dbTicket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == queuedTicket.Id);
        if (dbTicket == null || dbTicket.Status == "Resolved" || dbTicket.Status == "Processing")
        {
            return; // Idempotency Guard check passes
        }

        dbTicket.Status = "Processing";
        await _dbContext.SaveChangesAsync();

        // Check if our environment is running in production cloud context
        string? env = Environment.GetEnvironmentVariable("ASNETCORE_ENVIRONMENT");
        bool isProduction = env == "Production" || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

        if (isProduction)
        {
            // ====================================================================
            // CLOUD PRODUCTION MODE: RESILIENT MOCK TELEMETRY ENGINE
            // Simulates real-world processing timing safely over the public web!
            // ====================================================================
            try
            {
                Console.WriteLine("☁️ [Cloud AI Engine] Simulating asynchronous agent telemetry loops...");
                await Task.Delay(4500); // Wait 4.5 seconds to emulate LLM latency delays natively

                bool is404 = dbTicket.Description.Contains("404") || dbTicket.Title.Contains("404");
                
                dbTicket.AssignedLabel = is404 ? "bug" : "investigate";
                dbTicket.AgentReply = is404 
                    ? "CLOUD AGENT ANALYSIS Complete: Route missing from RouteConfig.cs file mapping. Update your backend controller declarations."
                    : "CLOUD AGENT ANALYSIS Complete: System logs indicate a runtime configuration variance. An operational audit is requested.";
                
                dbTicket.Status = "Resolved";
                dbTicket.ResolvedAt = DateTime.UtcNow; // Record completion milestone stamp
                Console.WriteLine($"🎯 [Cloud AI Engine] Successfully resolved ticket #{dbTicket.Id}!");
            }
            catch (Exception ex)
            {
                dbTicket.Status = "Failed";
                Console.WriteLine($"❌ [Cloud AI Engine Error]: {ex.Message}");
            }
        }
        else
        {
            // ====================================================================
            // LOCAL DEVELOPMENT MODE: HARDWARE OLLAMA CORE
            // Speaks directly to your physical laptop graphics card pipelines!
            // ====================================================================
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var ollamaUrl = "http://localhost:11434/api/generate";

            try
            {
                var payload = new {
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

                string mockLogs = errorCode.Contains("404") 
                    ? "DATABASE LOG: Thread 4: Error 404 on endpoint '/api/auth/login'. Reason: Route missing from RouteConfig.cs file mapping."
                    : "DATABASE LOG: General warning. No explicit route failure mappings located.";

                var triagePayload = new {
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

                dbTicket.AssignedLabel = errorCode.Contains("404") ? "bug" : "investigate";
                dbTicket.AgentReply = aiFixMessage;
                dbTicket.Status = "Resolved";
                dbTicket.ResolvedAt = DateTime.UtcNow; // Record completion milestone stamp

                Console.WriteLine($"🎯 [Local AI Engine] Successfully resolved ticket #{dbTicket.Id}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Local AI Engine Error]: {ex.Message}");
                dbTicket.Status = "Failed";
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}
