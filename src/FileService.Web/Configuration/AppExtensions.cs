using Framework.Middlewares;
using Serilog;

namespace FileService.Web.Configuration;

public static class AppExtension 
{
    public static async Task<WebApplication> ConfigureExtensions(this WebApplication app)
    {
        app.UseExceptionMiddleware();
        
        // Логирование HTTP запросов
        app.UseSerilogRequestLogging();
        
        app.UseSwagger();
        app.UseSwaggerUI();

        return app;
    }
}