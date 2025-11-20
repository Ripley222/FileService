using Serilog;
using Shared.Framework.Extensions;

namespace FileService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this IApplicationBuilder app)
    {
        app.UseExceptionMiddleware();
        
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}