using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Web.Configuration;

public static class Inject
{
    public static IServiceCollection ConfigureApp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddCore(configuration)
            .AddS3(configuration)
            .AddPostgres(configuration)
            .AddSwaggerGen()
            .AddControllers();
        
        // Убрать стандартный возврат ответа ошибок от AspNetCore.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddHealthChecks()
            .AddCheck<S3HealthCheck>("s3-storage")
            .AddNpgSql(configuration.GetConnectionString("FileServiceDb")!, name: "postgres");
        
        return services;
    }
}