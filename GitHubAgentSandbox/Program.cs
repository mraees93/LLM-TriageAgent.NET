using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

// ====================================================================
// PHASE 1 ENGINE RUNNER (HTTP FALLBACK LAYER)
// ====================================================================

Console.WriteLine("🆕 Incoming Customer Ticket: \"Issue #2045: Users are reporting a 404 error page whenever they attempt to reach the login screen.\"");
Console.WriteLine("⏳ Starting Agent Reasoning Loop via Local Network...");

// 1. Manually build the reasoning context to pass to our tools
var toolInstance = new GitHubToolsPlugin();

// 2. Setup standard .NET HttpClient to speak with your local Ollama server
using var client = new HttpClient();
var ollamaUrl = "http://localhost:11434/api/generate";

// 3. Craft a strict prompt to extract the error code from the ticket
var payload = new
{
    model = "phi3",
    prompt = "Extract ONLY the numerical error code from this ticket: 'Issue #2045: Users are reporting a 404 error page'. Respond with just the number.",
    stream = false
};

try
{
    // Send request to your local Ollama app
    var jsonPayload = JsonSerializer.Serialize(payload);
    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
    var response = await client.PostAsync(ollamaUrl, content);
    var responseString = await response.Content.ReadAsStringAsync();
    
    using var doc = JsonDocument.Parse(responseString);
    string errorCode = doc.RootElement.GetProperty("response").GetString()?.Trim() ?? "404";

    // 4. STEP 1 (Tool Use): Run your separate log lookup tool
    string logs = await toolInstance.GetSystemLogsAsync(errorCode);

    // 5. STEP 2 (Reasoning): Let the model read the logs and draft the triage action
    var triagePayload = new
    {
        model = "phi3",
        prompt = $"Based on these system logs: '{logs}', draft a short 1-sentence fix message for the user. Do not include extra text.",
        stream = false
    };

    var triageJson = JsonSerializer.Serialize(triagePayload);
    var triageContent = new StringContent(triageJson, Encoding.UTF8, "application/json");
    var triageResponse = await client.PostAsync(ollamaUrl, triageContent);
    var triageResponseString = await triageResponse.Content.ReadAsStringAsync();
    
    using var triageDoc = JsonDocument.Parse(triageResponseString);
    string aiFixMessage = triageDoc.RootElement.GetProperty("response").GetString()?.Trim() ?? "Route config fix required.";

    // 6. STEP 3 (Multi-step Action): Trigger your separate execution tool
    await toolInstance.ApplyLabelAndReplyAsync("2045", "bug", aiFixMessage);

}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Connection Error: Ensure the Ollama app is running in your taskbar! Details: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine("🏁 Task complete. Agent sandbox process finished successfully.");
