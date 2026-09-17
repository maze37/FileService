using Amazon.S3;
using FileService.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public static class Inject
{
    public static IServiceCollection AddS3(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(nameof(S3Options)));

        // Внутренний клиент - для всех операций из backend
        services.AddSingleton<IAmazonS3>(sp =>
        {
            S3Options s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.Endpoint, 
                ForcePathStyle = true,
                UseHttp = !s3Options.WithSsl
            };

            return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
        });
        
        // Presign-клиент - подписывает URL под внешний хост, который увидит браузер
        services.AddKeyedSingleton<IAmazonS3>(S3ClientKeys.PRESIGN, (sp, _) =>
        {
            var s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.ExternalEndpoint,
                ForcePathStyle = true,
                UseHttp = !s3Options.WithSsl
            };
            return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
        });
        
        services.AddSingleton<IS3Provider, S3Provider>();
        
        services.AddHostedService<S3BucketInitializer>();

        services.AddTransient<IChunkSizeCalculator, ChunkSizeCalculator>();
        
        return services;
    }
}