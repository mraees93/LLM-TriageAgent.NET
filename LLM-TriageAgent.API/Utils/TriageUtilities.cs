using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LLM_TriageAgent.API.Utils;

public static class TriageUtilities
{
    public static int? ExtractHttpErrorCode(string title, string description)
    {
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description)) return null;
        
        string fullTextSearch = $"{title} {description}";
        var numericMatches = Regex.Matches(fullTextSearch, @"\b\d{3}\b");
        
        foreach (Match match in numericMatches)
        {
            if (int.TryParse(match.Value, out int codeValue))
            {
                if (codeValue >= 400 && codeValue <= 599)
                {
                    return codeValue; 
                }
            }
        }
        return null;
    }

    public static (string Label, string Reply) GetProductionMockResponse(int? errorCode, string description)
    {
        string text = description?.ToLower() ?? "";
        bool containsKeyword = text.Contains("bug") || text.Contains("crash") || text.Contains("nullreference");

        if (errorCode == null)
        {
            if (containsKeyword)
            {
                return ("bug", "CLOUD AI RESOLUTION: Core process crash indicators identified in telemetry stream. System logs trace a thread exception block. Patch applied to program execution stack.");
            }
            return ("investigate", "CLOUD AI RESOLUTION: General system warning flags identified. Initiating standard systems architecture operational diagnostics audit.");
        }

        var responseMatrix = new Dictionary<int, (string Label, string Reply)>
        {
            { 400, ("bug", "CLOUD AI RESOLUTION: HTTP 400 Bad Request. Intercepted malformed JSON object schema headers inside incoming payload. Client validation pipeline parameters reset.") },
            { 401, ("investigate", "CLOUD AI RESOLUTION: HTTP 401 Unauthorized. Telemetry detects expired JWT bearer keys on infrastructure access tokens. Triggering credential reset handshake.") },
            { 403, ("investigate", "CLOUD AI RESOLUTION: HTTP 403 Forbidden. Client signature validation failed against backend security boundary filters. Access revoked for engineering review.") },
            { 404, ("bug", "CLOUD AI RESOLUTION: HTTP 404 Not Found. Detected broken route endpoint configurations. Missing mapping parameter has been patched inside RouteConfig.cs.") },
            { 405, ("bug", "CLOUD AI RESOLUTION: HTTP 405 Method Not Allowed. Client framework dispatched a mismatching HTTP verb to an unmapped API endpoint state layer.") },
            { 429, ("investigate", "CLOUD AI RESOLUTION: HTTP 429 Too Many Requests. Client traffic hit algorithmic Fixed Window rate limits. Temporarily throttling client gateway access channels.") },
            { 500, ("bug", "CLOUD AI RESOLUTION: HTTP 500 Internal Server Error. Deep table trace logs isolate an active NullReferenceException loop. Safe handling layer injected into DB context data layers.") },
            { 502, ("investigate", "CLOUD AI RESOLUTION: HTTP 502 Bad Gateway. Public proxy node failed to establish a network socket handshake with the containerized backend daemon instance.") },
            { 503, ("investigate", "CLOUD AI RESOLUTION: HTTP 503 Service Unavailable. System node pools hit compute latency ceilings under high packet loads. Spinning up automated container scale groups.") }
        };

        if (responseMatrix.TryGetValue(errorCode.Value, out var matchedPayload))
        {
            return matchedPayload;
        }

        string fallbackLabel = errorCode.Value >= 500 ? "bug" : "investigate";
        return (fallbackLabel, $"CLOUD AI RESOLUTION: Detected active HTTP status code {errorCode.Value} inside DevOps stream log. Initializing comprehensive systems tracing protocols.");
    }

    // AUTOMATED SRE SYSTEM PROMPT BUILDER
    public static string GenerateAiPrompt(int? errorCode, string title, string description)
    {
        string header = "[SYSTEM CONTEXT: Automated SRE Core Triage Agent]\n" +
                        "Analyze the following DevOps incident telemetry report.\n";

        if (errorCode != null)
        {
            header = "[SYSTEM CONTEXT: Automated SRE Core Triage Agent]\n" +
                     $"Analyze the following DevOps incident telemetry report containing explicit HTTP Status Fault Code {errorCode.Value}.\n";
        }

        return $"{header}" +
               "1. Classify the root cause category string parameter as strictly either 'bug' or 'investigate'.\n" +
               "2. Generate a concise, single-sentence infrastructure resolution engineering note.\n" +
               $"[DATA PAYLOAD]\nTitle: {title}\nTelemetry Log: {description}";
    }
}
