using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Service", "devsecops-sftp-service")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({MachineName}) {Message:lj} {Properties}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
    .MinimumLevel.Information()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR");
var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");

if (string.IsNullOrEmpty(vaultAddress) || string.IsNullOrEmpty(vaultToken))
{
    throw new InvalidOperationException("Vault address or token is not provided in the environment variables.");
}

builder.Services.AddSingleton(new SftpService(vaultAddress, vaultToken));
builder.Host.UseSerilog();
builder.Services.AddAllElasticApm();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();

var app = builder.Build();

try
{
    var authMethod = new TokenAuthMethodInfo(vaultToken);
    var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
    var vaultClient = new VaultClient(vaultClientSettings);

    var result = vaultClient.V1.Secrets.KeyValue.V2
        .ReadSecretAsync("source", mountPoint: "devsecops-sftp-service")
        .GetAwaiter().GetResult();

    var secretData = result.Data.Data;

    Console.WriteLine("Fetched secret from Vault:");
    foreach (var kvp in secretData)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error fetching secret: {ex.Message}");
}

app.UseSerilogRequestLogging();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
