using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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
        Console.WriteLine($"\n [Queue Consumer] Picked up Ticket #{queuedTicket.Id} from the message queue.");

        var dbTicket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == queuedTicket.Id);
        
        if (dbTicket == null || dbTicket.Status == "Resolved" || dbTicket.Status == "Processing")
        {
            return; // Idempotency check passed safely
        }

        dbTicket.Status = "Processing";
        await _dbContext.SaveChangesAsync();

        // Check if our environment is running in production cloud context
        string? databaseEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        bool isProductionCloud = !string.IsNullOrEmpty(databaseEnv) && databaseEnv.Contains("Host=");

        if (isProductionCloud)
        {
            // CLOUD PRODUCTION MOCK AI ENGINE, simulates local Ollama response timing safely over the public web
            try
            {
                Console.WriteLine("[Cloud AI Agent] Emulating background tool analysis loops...");
                
                int dynamicDelayMs = new Random().Next(3500, 7500);
                await Task.Delay(dynamicDelayMs); 

                //bool contains404 = dbTicket.Description.Contains("404") || dbTicket.Title.Contains("404");
                bool hasHttpErrorCode = false;
                var fullTicketText = $"{dbTicket.Title} {dbTicket.Description}";

                // Matches any standalone 3-digit numeric string boundary patterns
                var numericMatches = Regex.Matches(fullTicketText, @"\b\d{3}\b");

                foreach (Match match in numericMatches)
                {
                if (int.TryParse(match.Value, out int codeValue))
                    {
                    if (codeValue >= 400 && codeValue <= 599)
                    {
                        hasHttpErrorCode = true;
                        break;
                    }
                    }
                }

                bool isSoftwareBug = hasHttpErrorCode || 
                             dbTicket.Description.ToLower().Contains("bug") || 
                             dbTicket.Description.ToLower().Contains("crash");

                if (isSoftwareBug)
                {
                    dbTicket.Status = "Resolved";
                    dbTicket.AssignedLabel = "bug";
                    dbTicket.AgentReply = "CLOUD AI RESOLUTION: Detected broken route endpoint configurations. Missing mapping parameter has been patched inside RouteConfig.cs.";
                }
                else
                {
                    dbTicket.Status = "Resolved";
                    dbTicket.AssignedLabel = "investigate";
                    dbTicket.AgentReply = "CLOUD AI RESOLUTION: General warning trace flags identified. Initiating standard systems architecture operational diagnostics audit.";
                }

                dbTicket.ResolvedAt = DateTime.UtcNow;
                Console.WriteLine($"[Cloud AI Agent] Successfully resolved ticket #{dbTicket.Id}!");

            }
            catch (Exception ex)
            {
                dbTicket.Status = "Failed";
                Console.WriteLine($"[Cloud AI Agent Error]: {ex.Message}");
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

                Console.WriteLine($"[AI Agent Tool Use] Searching backend logs for code identifier: '{errorCode}'...");
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
                dbTicket.ResolvedAt = DateTime.UtcNow;

                Console.WriteLine($"[Local AI Agent Action] Successfully resolved ticket #{dbTicket.Id}!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Queue Consumer Error] Processing failed. Details: {ex.Message}");
                dbTicket.Status = "Failed";
            }
        }

        await _dbContext.SaveChangesAsync();
    }
}
