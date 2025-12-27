using System.Globalization;
using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using FileService.Web.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    var environment = builder.Environment.EnvironmentName;
    builder.Configuration.AddJsonFile($"appsettings.{environment}.json", true, true);

    builder.Services.AddConfiguration(builder.Configuration);
    builder.Services.AddInfrastructurePostgres(builder.Configuration);
    builder.Services.AddS3Infrastructure(builder.Configuration);
    builder.Services.AddApplication();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
        await dbContext.Database.MigrateAsync();
    }

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