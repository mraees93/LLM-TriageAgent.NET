using Microsoft.EntityFrameworkCore;
using MassTransit;
using LLM_TriageAgent.API.Database;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. DATABASE CONFIGURATION (AUTOMATIC SWITCHING)
// ====================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    // Local development uses SQLite
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=triage.db"));
}
else
{
    // Production on Render uses Aiven PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// ====================================================================
// 2. MASSTRANSIT BACKGROUND QUEUE SETUP
// ====================================================================
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<LLM_TriageAgent.API.Services.TicketConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
