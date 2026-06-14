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
    // Local workspace uses lightweight SQLite file
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString ?? "Data Source=triage.db"));
}
else
{
    // FIXED: Reads your live cloud Aiven string from your Render variable environment settings!
    var prodConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
    
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(prodConnectionString));
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
// 3. CORS POLICY SETUP (GLOBAL OPEN GATEWAY PROTOTYPE)
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

// ====================================================================
// MIDDLEWARE STACK EXECUTION SEQUENCE
// Configures open origin policy definitions right before processing routes!
// ====================================================================
app.UseCors();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
