using SALLY_API;
using Microsoft.AspNetCore.Authentication.Negotiate;
using SALLY_API.Walker;
using SALLY_API.Entities;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var configuration = builder.Configuration;

// Get log file path and env file path from configuration based on the current environment
VersionInfo.EnvFilePath = configuration["EnvironmentSettings:EnvFilePath"];
VersionInfo.LogFilePath = configuration["Logging:LogFilePath"];
VersionInfo.IsScheduler = configuration.GetValue<bool>("AllowScheduler");

GlobalLogger.Initialize(VersionInfo.LogFilePath);
GlobalLogger.Logger.Debug("Logger Initialized");

DotEnv.Load(VersionInfo.EnvFilePath);
GlobalLogger.Logger.Debug("Scheduler allowed: " + VersionInfo.IsScheduler + "\nDotenv: " + VersionInfo.EnvFilePath + "\nLogfile: " + VersionInfo.LogFilePath);


// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin() // Allows any origin
               .AllowAnyMethod()  // Allows any HTTP method (GET, POST, etc.)
               .AllowAnyHeader(); // Allows any headers
    });
});





builder.Services.AddControllers();
builder.Services.AddSingleton<APIService>();

builder.Services.AddSingleton<InMemoryQueueService>();
builder.Services.AddHostedService<UserQueueWorker>(); // for upsertuser endpoint in badgeify

if (VersionInfo.IsScheduler) // only in prod
{
    Console.WriteLine("starting scheduler");
    builder.Services.AddHostedService<Scheduler>();
}


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
    
});

var app = builder.Build();
/*if (app.Environment.IsDevelopment())
{*/
    app.UseSwagger();
    app.UseSwaggerUI();
/*}*/

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();



