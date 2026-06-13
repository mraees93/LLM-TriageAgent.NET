using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

public class GitHubToolsPlugin
{
    // TOOL 1: Look up mock system logs based on what the user says
    [KernelFunction]
    [Description("Queries the database logs to find error codes or routing failures mentioned in a support ticket.")]
    public async Task<string> GetSystemLogsAsync(string errorCode)
    {
        Console.WriteLine($"\n🔍 [Tool Active] Searching backend database logs for code: '{errorCode}'...");
        await Task.Delay(800); // Simulate network lag

        if (errorCode.Contains("404"))
        {
            return "DATABASE LOG [10:14 AM]: Thread 4: Error 404 on endpoint '/api/auth/login'. Reason: Route missing from RouteConfig.cs file mapping.";
        }
        
        return "DATABASE LOG [11:00 AM]: Warning: No explicit application failures recorded for this code.";
    }

    // TOOL 2: Update the issue label and write a comment response
    [KernelFunction]
    [Description("Updates a GitHub issue by applying a classification label and posting a final technical reply message.")]
    public async Task ApplyLabelAndReplyAsync(string issueId, string assignedLabel, string replyMessage)
    {
        Console.WriteLine($"\n⚙️ [Tool Active] Accessing GitHub issue repository for ID: {issueId}...");
        await Task.Delay(800); // Simulate API connection

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n==================================================");
        Console.WriteLine($"🎯 SUCCESS: ACTIONS EXECUTED ON ISSUE {issueId}");
        Console.WriteLine($"🏷️  LABEL APPLIED : [{assignedLabel.ToUpper()}]");
        Console.WriteLine($"💬 POSTED REPLY  : \"{replyMessage}\"");
        Console.WriteLine("==================================================\n");
        Console.ResetColor();
    }
}
