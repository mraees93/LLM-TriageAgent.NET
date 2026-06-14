using Microsoft.EntityFrameworkCore;
using MassTransit;
using LLM_TriageAgent.API.Database; 
using Microsoft.Extensions.DependencyInjection; 

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. DATABASE CONFIGURATION (AUTOMATIC SWITCHING)
// ====================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=triage.db"));
}
else
{
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

// ====================================================================
// 3. CORS POLICY SETUP
// ====================================================================
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
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

// CRITICAL ORDER FIX: UseCors MUST sit exactly here!
// It must run before HttpsRedirection and MapControllers.
app.UseCors();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
