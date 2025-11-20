using System.Globalization;
using FileService.Web.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddConfiguration(builder.Configuration);
    
    var environment = builder.Environment.EnvironmentName;

    builder.Configuration.AddJsonFile($"appsettings.{environment}.json", true, true);

    var app = builder.Build();

    // Configure the HTTP request pipeline. 
    app.Configure();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}