using FileService.Core.Abstractions;
using FileService.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileService.Infrastructure.Postgres;

public static class Inject
{
    public static IServiceCollection AddPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FileServiceDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Database");
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            options.UseNpgsql(connectionString);
            options.UseLoggerFactory(loggerFactory);
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<FileServiceDbContext>());
        
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();

        services.AddScoped<IOutboxService, OutboxService>();
        
        return services;
    }
}