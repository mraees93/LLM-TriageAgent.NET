using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MassTransit;
using LLM_TriageAgent.API.Database;
using LLM_TriageAgent.API.Models;
using LLM_TriageAgent.API.Services;
using LLM_TriageAgent.API.Utils;

namespace LLM_TriageAgent.Tests;

public class ConsumerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // Helper layout method providing a clean baseline mock parameter for IMemoryCache
    private IMemoryCache GetMockMemoryCache()
    {
        var mockMemoryCache = new Mock<IMemoryCache>();
        var mockCacheEntry = new Mock<ICacheEntry>();
        
        // Configures TryGetValue out parameters to default to false (cache-miss routine simulation)
        object? cachedValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);
        mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);
        
        return mockMemoryCache.Object;
    }

    // TEST 1: EXISTING FAULT CONSUMER TEST
    [Fact]
    public async Task TicketFaultConsumer_Should_Quarantine_And_Fail_Ticket_When_Triggered()
    {
        using var dbContext = GetInMemoryDbContext();

        var testTicket = new SupportTicket
        {
            Id = "test-fault-id-123",
            Title = "Database Network Collision Traces",
            Description = "Persistent critical database loss timeouts",
            Status = "Processing"
        };

        dbContext.SupportTickets.Add(testTicket);
        await dbContext.SaveChangesAsync();

        var mockFaultContext = new Mock<ConsumeContext<Fault<SupportTicket>>>();
        var mockFaultMessage = new Mock<Fault<SupportTicket>>();

        mockFaultMessage.Setup(m => m.Message).Returns(testTicket);
        mockFaultContext.Setup(c => c.Message).Returns(mockFaultMessage.Object);

        var faultConsumer = new TicketFaultConsumer(dbContext);
        await faultConsumer.Consume(mockFaultContext.Object);

        var updatedTicket = await dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == "test-fault-id-123");

        Assert.NotNull(updatedTicket);
        Assert.Equal("Failed", updatedTicket.Status);
        Assert.Equal("SYSTEM_FAULT", updatedTicket.AssignedLabel);
    }

    // TEST 2: AI RE-TRIAGE "BUG" CLASSIFICATION RULE
    [Fact]
    public async Task TicketConsumer_Should_Assign_Bug_Label_When_Description_Contains_404()
    {
        // Force the consumer to run its fast cloud mock path instead of crashing on local Ollama ports
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=fake_aiven_postgres_pool");

        using var dbContext = GetInMemoryDbContext();

        var testTicket = new SupportTicket
        {
            Id = "ticket-404-id",
            Title = "Broken Route Link",
            Description = "Getting a nasty 404 error page on checkout screen links",
            Status = "Pending"
        };

        dbContext.SupportTickets.Add(testTicket);
        await dbContext.SaveChangesAsync();

        var mockConsumeContext = new Mock<ConsumeContext<SupportTicket>>();
        mockConsumeContext.Setup(c => c.Message).Returns(testTicket);

        var consumer = new TicketConsumer(dbContext);
        await consumer.Consume(mockConsumeContext.Object);

        var updatedTicket = await dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == "ticket-404-id");

        Assert.NotNull(updatedTicket);
        Assert.Equal("Resolved", updatedTicket.Status);
        Assert.Equal("bug", updatedTicket.AssignedLabel);
        Assert.Contains("RouteConfig.cs", updatedTicket.AgentReply);

        // Clean up environment variables so other tests stay isolated
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
    }

    // TEST 3: AI RE-TRIAGE "INVESTIGATE" CLASSIFICATION RULE
    [Fact]
    public async Task TicketConsumer_Should_Assign_Investigate_Label_For_Generic_Errors()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=fake_aiven_postgres_pool");

        using var dbContext = GetInMemoryDbContext();

        var testTicket = new SupportTicket
        {
            Id = "ticket-generic-id",
            Title = "General CPU Warning",
            Description = "Hardware temperature telemetry reports brief brief spike spikes during background tasks.",
            Status = "Pending"
        };

        dbContext.SupportTickets.Add(testTicket);
        await dbContext.SaveChangesAsync();

        var mockConsumeContext = new Mock<ConsumeContext<SupportTicket>>();
        mockConsumeContext.Setup(c => c.Message).Returns(testTicket);

        var consumer = new TicketConsumer(dbContext);
        await consumer.Consume(mockConsumeContext.Object);

        var updatedTicket = await dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == "ticket-generic-id");

        Assert.NotNull(updatedTicket);
        Assert.Equal("Resolved", updatedTicket.Status);
        Assert.Equal("investigate", updatedTicket.AssignedLabel);
        Assert.Contains("diagnostics audit", updatedTicket.AgentReply);

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
    }

    // TEST 4: IDEMPOTENCY SAFETY SHIELD GUARD
    [Fact]
    public async Task TicketConsumer_Should_Skip_Processing_If_Ticket_Is_Already_Resolved()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=fake_aiven_postgres_pool");

        using var dbContext = GetInMemoryDbContext();

        var testTicket = new SupportTicket
        {
            Id = "idempotent-id-999",
            Title = "Old Completed Task",
            Description = "404 route link broken",
            Status = "Resolved", // Setting status to Resolved beforehand
            AssignedLabel = "manual_fix",
            AgentReply = "Original Human Intervention Completed."
        };

        dbContext.SupportTickets.Add(testTicket);
        await dbContext.SaveChangesAsync();

        var mockConsumeContext = new Mock<ConsumeContext<SupportTicket>>();
        mockConsumeContext.Setup(c => c.Message).Returns(testTicket);

        var consumer = new TicketConsumer(dbContext);
        await consumer.Consume(mockConsumeContext.Object);

        var currentTicket = await dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == "idempotent-id-999");

        // The consumer should exit early and preserve the original values instead of overwriting them
        Assert.NotNull(currentTicket);
        Assert.Equal("Resolved", currentTicket.Status);
        Assert.Equal("manual_fix", currentTicket.AssignedLabel);
        Assert.Equal("Original Human Intervention Completed.", currentTicket.AgentReply);

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
    }

    [Fact]
    public void TriageUtilities_Should_Extract_Valid_HTTP_Codes_And_Ignore_Invalid_Numbers()
    {
        // Act: Test standard server fault codes
        int? validCode = TriageUtilities.ExtractHttpErrorCode("Server Error", "Critical failure throwing an HTTP 502 code.");
        int? outOfBoundsCode = TriageUtilities.ExtractHttpErrorCode("User Update", "Processed payload for user account ID 250 safely.");

        Assert.NotNull(validCode);
        Assert.Equal(502, validCode.Value);
        Assert.Null(outOfBoundsCode);
    }

    [Fact]
    public void TriageUtilities_Should_Map_Rate_Limiter_Payloads_Correctly()
    {
        // Act: Simulate an HTTP 429 text description string match
        var (label, reply) = TriageUtilities.GetProductionMockResponse(429, "Throttling active.");

        Assert.Equal("investigate", label);
        Assert.Contains("Fixed Window rate limits", reply);
        Assert.Contains("throttling client gateway", reply);
    }

    // TEST 7: IN-MEMORY CACHE-ASIDE ALLOCATION VALIDATION
    [Fact]
    public async Task GetAllTickets_Should_Populate_Cache_On_First_Load()
    {
        using var dbContext = GetInMemoryDbContext();
        var mockPublishEndpoint = new Mock<IPublishEndpoint>();
        
        var mockMemoryCache = new Mock<IMemoryCache>();
        var mockCacheEntry = new Mock<ICacheEntry>();
        mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);

        var controller = new API.Controllers.TicketsController(dbContext, mockPublishEndpoint.Object, mockMemoryCache.Object);
        var result = await controller.GetAllTickets();

        mockMemoryCache.Verify(m => m.CreateEntry(It.IsAny<object>()), Times.Once);
    }
}
