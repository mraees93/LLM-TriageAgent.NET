using Microsoft.EntityFrameworkCore;
using MassTransit;
using LLM_TriageAgent.API.Database; 
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.AspNetCore.HttpOverrides;
using LLM_TriageAgent.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

// 1. PROXY PORT FORWARDING CONFIGURATION 
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    
    options.KnownIPNetworks.Clear(); 
    options.KnownProxies.Clear();
});

// 2. DUAL-SOURCE CONNECTION CHECK
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

// 3. HARDENED DB CONFIGURATION WITH AUTOMATIC ENGINE SWITCHING
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Host="))
    {
        Console.WriteLine(">>>> SYSTEM CHECK: EXTERNAL POSTGRES DETECTED <<<<");
        // Force version 16 to ensure compatibility with Aiven/Modern engines
        options.UseNpgsql(connectionString, o => o.SetPostgresVersion(16, 0));
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
    else
    {
        Console.WriteLine(">>>> SYSTEM CHECK: FALLING BACK TO SQLITE <<<<");
        options.UseSqlite(connectionString ?? "Data Source=triage.db");
    }
});

// MASSTRANSIT BACKGROUND QUEUE SETUP
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<LLM_TriageAgent.API.Services.TicketConsumer>();
    x.AddConsumer<TicketFaultConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

// CORS POLICY SETUP (GLOBAL OPEN GATEWAY PROTOTYPE)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().SetPreflightMaxAge(TimeSpan.FromMinutes(10));;
    });
});

builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// MIDDLEWARE STACK EXECUTION SEQUENCE
app.UseForwardedHeaders();
app.UseCors();
app.MapControllers();

// 4. SECURE AUTOMATIC MIGRATION LOGIC
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Console.WriteLine(">>>> SYSTEM CHECK: STARTING DATABASE MIGRATIONS <<<<");
        db.Database.Migrate();
        Console.WriteLine(">>>> SYSTEM CHECK: MIGRATIONS COMPLETE <<<<");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>>> SYSTEM CHECK: MIGRATION ERROR: {ex.Message} <<<<");
    }
}

app.Run();
